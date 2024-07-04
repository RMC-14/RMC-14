using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Input;
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

namespace Content.Shared._RMC14.Inventory;

public abstract class SharedCMInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
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

        SubscribeLocalEvent<CMItemSlotsComponent, MapInitEvent>(OnSlotsFillMapInit);
        SubscribeLocalEvent<CMItemSlotsComponent, AfterAutoHandleStateEvent>(OnSlotsComponentHandleState);
        SubscribeLocalEvent<CMItemSlotsComponent, ActivateInWorldEvent>(OnSlotsActivateInWorld);
        SubscribeLocalEvent<CMItemSlotsComponent, ItemSlotEjectAttemptEvent>(OnSlotsEjectAttempt);
        SubscribeLocalEvent<CMItemSlotsComponent, EntInsertedIntoContainerMessage>(OnSlotsEntInsertedIntoContainer);
        SubscribeLocalEvent<CMItemSlotsComponent, EntRemovedFromContainerMessage>(OnSlotsEntRemovedFromContainer);

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
                        OnHolster(entity, 3, CMHolsterChoose.Last);
                }, handle: false))
            .Register<SharedCMInventorySystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedCMInventorySystem>();
    }

    private void AllowUnholster<T>(Entity<T> ent, ref IsUnholsterableEvent args) where T : IComponent?
    {
        args.Unholsterable = true;
    }

    private void OnSlotsFillMapInit(Entity<CMItemSlotsComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Slot is not { } slot || ent.Comp.Count is not { } count)
            return;

        var itemId = ent.Comp.StartingItem;
        var slots = EnsureComp<ItemSlotsComponent>(ent);
        var coordinates = Transform(ent).Coordinates;
        for (var i = 0; i < count; i++)
        {
            var n = i + 1;
            var copy = new ItemSlot(slot);
            copy.Name = $"{copy.Name} {n}";

            _itemSlots.AddItemSlot(ent, $"{slot.Name}{n}", copy);

            if (itemId != null)
            {
                if (copy.ContainerSlot is { } containerSlot)
                {
                    var item = Spawn(itemId, coordinates);
                    _container.Insert(item, containerSlot);
                }
                else
                {
                    copy.StartingItem = itemId;
                }
            }
        }

        ContentsUpdated(ent);
        Dirty(ent, slots);
    }

    private void OnSlotsComponentHandleState(Entity<CMItemSlotsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ContentsUpdated(ent);
    }

    private void OnSlotsActivateInWorld(Entity<CMItemSlotsComponent> ent, ref ActivateInWorldEvent args)
    {
        PickupSlot(args.User, ent);
    }

    private void OnSlotsEjectAttempt(Entity<CMItemSlotsComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Cooldown is { } cooldown &&
            _timing.CurTime < ent.Comp.LastEjectAt + cooldown)
        {
            args.Cancelled = true;
        }
    }

    protected void OnSlotsEntInsertedIntoContainer(Entity<CMItemSlotsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        ContentsUpdated(ent);
    }

    protected void OnSlotsEntRemovedFromContainer(Entity<CMItemSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!_timing.ApplyingState)
        {
            ent.Comp.LastEjectAt = _timing.CurTime;
            Dirty(ent);
        }

        ContentsUpdated(ent);
    }

    protected virtual void ContentsUpdated(Entity<CMItemSlotsComponent> ent)
    {
        var (filled, total) = GetItemSlotsFilled(ent.Owner);
        CMItemSlotsVisuals visuals;
        if (total == 0)
            visuals = CMItemSlotsVisuals.Empty;
        else if (filled >= total)
            visuals = CMItemSlotsVisuals.Full;
        else if (filled >= total * 0.666f)
            visuals = CMItemSlotsVisuals.High;
        else if (filled >= total * 0.333f)
            visuals = CMItemSlotsVisuals.Medium;
        else if (filled > 0)
            visuals = CMItemSlotsVisuals.Low;
        else
            visuals = CMItemSlotsVisuals.Empty;

        _appearance.SetData(ent, CMItemSlotsLayers.Fill, visuals);
    }

    private bool SlotCanInteract(EntityUid user, EntityUid holster, [NotNullWhen(true)] out ItemSlotsComponent? itemSlots)
    {
        if (!TryComp(holster, out itemSlots))
            return false;

        // no quick unholstering other's holsters
        if (_container.TryGetContainingContainer((holster, null), out var container) &&
            container.Owner != user &&
            _inventory.HasSlot(container.Owner, container.ID))
        {
            itemSlots = default;
            return false;
        }

        return true;
    }

    private bool InsertSlot(EntityUid user, Entity<CMItemSlotsComponent> holster, EntityUid item)
    {
        if (!SlotCanInteract(user, holster, out var itemSlots))
            return false;

        return _itemSlots.TryInsertEmpty((holster, itemSlots), item, user, true);
    }

    private bool PickupSlot(EntityUid user, EntityUid holster)
    {
        if (!SlotCanInteract(user, holster, out var itemSlots))
            return false;

        foreach (var slot in itemSlots.Slots.Values.OrderBy(s => s.Priority))
        {
            if (_itemSlots.TryEjectToHands(holster, slot, user, true))
                return true;
        }

        return false;
    }

    private void OnHolster(EntityUid user, int startIndex, CMHolsterChoose choose = CMHolsterChoose.First)
    {
        if (_hands.TryGetActiveItem(user, out var active))
        {
            Holster(user, active.Value);
            return;
        }

        Unholster(user, startIndex, choose);
    }

    private void Holster(EntityUid user, EntityUid item)
    {
        // TODO RMC14 try uniform-attached weapon and ammo holsters first
        foreach (var flag in _quickEquipOrder)
        {
            var slots = _inventory.GetSlotEnumerator(user, flag);
            while (slots.MoveNext(out var slot))
            {
                if (HasComp<CMHolsterComponent>(slot.ContainedEntity) &&
                    TryComp(slot.ContainedEntity, out CMItemSlotsComponent? holster) &&
                    InsertSlot(user, (slot.ContainedEntity.Value, holster), item))
                {
                    return;
                }

                if (slot.ContainedEntity != null)
                    continue;

                if (_inventory.TryEquip(user, item, slot.ID, true))
                    return;
            }
        }

        _popup.PopupClient(Loc.GetString("cm-inventory-unable-equip"), user, user, PopupType.SmallCaution);
    }

    private void Unholster(EntityUid user, int startIndex, CMHolsterChoose choose)
    {
        if (_order.Length == 0)
            return;

        if (startIndex >= _order.Length)
            startIndex = _order.Length - 1;

        for (var i = startIndex; i < _order.Length; i++)
        {
            if (Unholster(user, _order[i], choose, out var stop) || stop)
                return;
        }

        for (var i = 0; i < startIndex; i++)
        {
            if (Unholster(user, _order[i], choose, out var stop) || stop)
                return;
        }
    }

    private bool Unholster(EntityUid user, SlotFlags flag, CMHolsterChoose choose, out bool stop)
    {
        stop = false;
        var enumerator = _inventory.GetSlotEnumerator(user, flag);

        if (choose == CMHolsterChoose.Last)
        {
            var items = new List<EntityUid>();
            while (enumerator.NextItem(out var next))
            {
                items.Add(next);
            }

            items.Reverse();

            foreach (var item in items)
            {
                if (Unholster(user, item, out stop))
                    return true;
            }
        }
        while (enumerator.NextItem(out var item))
        {
            if (Unholster(user, item, out stop))
                return true;
        }

        return false;
    }

    private bool Unholster(EntityUid user, EntityUid item, out bool stop)
    {
        stop = false;
        if (HasComp<CMHolsterComponent>(item))
        {
            if (TryComp(item, out CMItemSlotsComponent? holster) &&
                holster.Cooldown is { } cooldown &&
                _timing.CurTime < holster.LastEjectAt + cooldown)
            {
                stop = true;
                _popup.PopupPredicted(holster.CooldownPopup, user, user, PopupType.SmallCaution);
                return false;
            }

            if (PickupSlot(user, item))
                return true;
        }

        var ev = new IsUnholsterableEvent();
        RaiseLocalEvent(item, ref ev);

        if (!ev.Unholsterable)
            return false;

        return _hands.TryPickup(user, item);
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

    public (int Filled, int Total) GetItemSlotsFilled(Entity<ItemSlotsComponent?> slots)
    {
        if (!Resolve(slots, ref slots.Comp, false))
            return (0, 0);

        var total = slots.Comp.Slots.Count;
        if (total == 0)
            return (0, 0);

        var filled = 0;
        foreach (var (_, slot) in slots.Comp.Slots)
        {
            if (slot.ContainerSlot?.ContainedEntity is { } contained &&
                !TerminatingOrDeleted(contained))
            {
                filled++;
            }
        }

        return (filled, slots.Comp.Slots.Count);
    }
}
