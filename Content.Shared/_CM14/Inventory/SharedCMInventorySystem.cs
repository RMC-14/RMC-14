using System.Linq;
using Content.Shared._CM14.Input;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Inventory;

public abstract class SharedCMInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SlotFlags[] _order =
    [
        SlotFlags.SUITSTORAGE, SlotFlags.BELT, SlotFlags.BACK,
        SlotFlags.POCKET, SlotFlags.INNERCLOTHING, SlotFlags.FEET
    ];

    private readonly SlotFlags[] _quickEquipOrder =
    [
        SlotFlags.BACK,
        SlotFlags.IDCARD,
        SlotFlags.INNERCLOTHING,
        SlotFlags.OUTERCLOTHING,
        SlotFlags.HEAD,
        SlotFlags.FEET,
        SlotFlags.MASK,
        SlotFlags.GLOVES,
        SlotFlags.EARS,
        SlotFlags.EYES,
        SlotFlags.BELT,
        SlotFlags.SUITSTORAGE,
        SlotFlags.NECK,
        SlotFlags.POCKET,
        SlotFlags.LEGS
    ];

    public override void Initialize()
    {
        SubscribeLocalEvent<GunComponent, IsUnholsterableEvent>(AllowUnholster);
        SubscribeLocalEvent<MeleeWeaponComponent, IsUnholsterableEvent>(AllowUnholster);

        SubscribeLocalEvent<CMHolsterComponent, MapInitEvent>(OnHolsterMapInit);
        SubscribeLocalEvent<CMHolsterComponent, AfterAutoHandleStateEvent>(OnComponentHandleState);
        SubscribeLocalEvent<CMHolsterComponent, ActivateInWorldEvent>(OnHolsterActivateInWorld);
        SubscribeLocalEvent<CMHolsterComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<CMHolsterComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<CMHolsterComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMHolsterPrimary,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } entity)
                        OnHolster(entity, 0);
                }, handle: false))
            .Bind(CMKeyFunctions.CMHolsterSecondary,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } entity)
                        OnHolster(entity, 1);
                }, handle: false))
            .Bind(CMKeyFunctions.CMHolsterTertiary,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } entity)
                        OnHolster(entity, 2);
                }, handle: false))
            .Bind(CMKeyFunctions.CMHolsterQuaternary,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } entity)
                        OnHolster(entity, 3);
                }, handle: false))
            .Register<SharedCMInventorySystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<SharedCMInventorySystem>();
    }

    private void AllowUnholster<T>(Entity<T> ent, ref IsUnholsterableEvent args) where T : IComponent?
    {
        args.Unholsterable = true;
    }

    private void OnHolsterMapInit(Entity<CMHolsterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Slot is not { } slot || ent.Comp.Count is not { } count)
            return;

        var slots = EnsureComp<ItemSlotsComponent>(ent);
        for (var i = 0; i < ent.Comp.Count; i++)
        {
            var n = i + 1;
            var copy = new ItemSlot(slot);
            copy.Name = $"{copy.Name} {n}";

            _itemSlots.AddItemSlot(ent, $"{slot.ID}{n}", copy);
        }

        Dirty(ent, slots);
    }

    private void OnComponentHandleState(Entity<CMHolsterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ContentsUpdated(ent);
    }

    private void OnHolsterActivateInWorld(Entity<CMHolsterComponent> ent, ref ActivateInWorldEvent args)
    {
        PickupSlot(args.User, ent);
    }

    private void OnItemSlotEjectAttempt(Entity<CMHolsterComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Cooldown is { } cooldown &&
            _timing.CurTime < ent.Comp.LastEjectAt + cooldown)
        {
            args.Cancelled = true;
        }
    }

    protected void OnEntInsertedIntoContainer(Entity<CMHolsterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        ContentsUpdated(ent);
    }

    protected void OnEntRemovedFromContainer(Entity<CMHolsterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        ent.Comp.LastEjectAt = _timing.CurTime;
        Dirty(ent);
        ContentsUpdated(ent);
    }

    protected virtual void ContentsUpdated(Entity<CMHolsterComponent> ent)
    {
    }

    private bool PickupSlot(EntityUid user, Entity<CMHolsterComponent> holster)
    {
        if (!TryComp(holster, out ItemSlotsComponent? itemSlots))
            return false;

        // no quick unholstering other's holsters
        if (_container.TryGetContainingContainer(holster.Owner, out var container) &&
            container.Owner != user &&
            _inventory.HasSlot(container.Owner, container.ID))
        {
            return false;
        }

        foreach (var slot in itemSlots.Slots.Values.OrderBy(s => s.Priority))
        {
            if (_itemSlots.TryEjectToHands(holster, slot, user, true))
                return true;
        }

        return false;
    }

    private void OnHolster(EntityUid user, int startIndex)
    {
        if (_hands.TryGetActiveItem(user, out var active))
        {
            Holster(user, active.Value);
            return;
        }

        Unholster(user, startIndex);
    }

    private void Holster(EntityUid user, EntityUid item)
    {
        // TODO CM14 try uniform-attached weapon and ammo holsters first
        foreach (var flag in _quickEquipOrder)
        {
            var slots = _inventory.GetSlotEnumerator(user, flag);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                    continue;

                // TODO CM14 try in-holster first
                if (_inventory.TryEquip(user, item, slot.ID, true))
                    return;
            }
        }

        _popup.PopupClient("You are unable to equip that.", user, user, PopupType.SmallCaution);
    }

    private void Unholster(EntityUid user, int startIndex)
    {
        if (_order.Length == 0)
            return;

        if (startIndex >= _order.Length)
            startIndex = _order.Length - 1;

        for (var i = startIndex; i < _order.Length; i++)
        {
            if (Unholster(user, _order[i]))
                return;
        }

        for (var i = 0; i < startIndex; i++)
        {
            if (Unholster(user, _order[i]))
                return;
        }
    }

    private bool Unholster(EntityUid user, SlotFlags flag)
    {
        var slots = _inventory.GetSlotEnumerator(user, flag);
        while (slots.NextItem(out var item))
        {
            if (TryComp(item, out CMHolsterComponent? holster) &&
                PickupSlot(user, (item, holster)))
            {
                return true;
            }

            var ev = new IsUnholsterableEvent();
            RaiseLocalEvent(item, ref ev);

            if (!ev.Unholsterable)
                continue;

            if (_hands.TryPickup(user, item))
                return true;
        }

        return false;
    }

    public bool TryEquipClothing(EntityUid user, Entity<ClothingComponent> clothing)
    {
        foreach (var order in _quickEquipOrder)
        {
            if ((clothing.Comp.Slots & order) == 0)
                continue;

            if (!_inventory.TryGetContainerSlotEnumerator(user, out var slots, clothing.Comp.Slots))
                continue;

            while (slots.MoveNext(out var slot))
            {
                if (_inventory.TryEquip(user, clothing, slot.ID))
                    return true;
            }
        }

        return false;
    }
}
