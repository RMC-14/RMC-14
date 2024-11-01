using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Input;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Inventory;

public abstract class SharedCMInventorySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

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

        SubscribeLocalEvent<CMHolsterComponent, GetVerbsEvent<AlternativeVerb>>(OnHolsterGetAltVerbs);
        SubscribeLocalEvent<CMHolsterComponent, AfterAutoHandleStateEvent>(OnHolsterComponentHandleState);
        SubscribeLocalEvent<CMHolsterComponent, EntInsertedIntoContainerMessage>(OnHolsterEntInsertedIntoContainer);
        SubscribeLocalEvent<CMHolsterComponent, EntRemovedFromContainerMessage>(OnHolsterEntRemovedFromContainer);

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

    private void OnHolsterGetAltVerbs(EntityUid holster, CMHolsterComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (comp.Contents.Count == 0)
            return;

        AlternativeVerb holsterVerb = new()
        {
            Act = () => Unholster(args.User, holster, out _),
            Text = Loc.GetString("rmc-holster-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"))
        };
        args.Verbs.Add(holsterVerb);
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

    private void OnHolsterComponentHandleState(Entity<CMHolsterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ContentsUpdated(ent);
    }

    private void OnSlotsActivateInWorld(Entity<CMItemSlotsComponent> ent, ref ActivateInWorldEvent args)
    {
        // If holster belongs to storage item, open it instead of unholstering
        if (HasComp<StorageComponent>(ent))
            return;

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

    protected void OnHolsterEntInsertedIntoContainer(Entity<CMHolsterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var item = args.Entity;
        var ev = new IsUnholsterableEvent();
        RaiseLocalEvent(item, ref ev);

        if (ev.Unholsterable &&                             // Check if unholsterable
            !ent.Comp.Contents.Contains(item) &&            // Here to prevent holster from counting one item twice
            (ent.Comp.Whitelist is not { } whitelist ||     // Check if no whitelist
            _whitelist.IsWhitelistPass(whitelist, item)))   //  or if item matches whitelist
            ent.Comp.Contents.Add(item);

        ContentsUpdated(ent);
    }

    protected void OnHolsterEntRemovedFromContainer(Entity<CMHolsterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!_timing.ApplyingState)
        {
            ent.Comp.LastEjectAt = _timing.CurTime;
            Dirty(ent);
        }

        var item = args.Entity;
        ent.Comp.Contents.Remove(item);

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

    protected virtual void ContentsUpdated(Entity<CMHolsterComponent> ent)
    {
        CMHolsterVisuals visuals = CMHolsterVisuals.Empty;
        var size = 0;

        // TODO: account for the gunslinger belt
        if (ent.Comp.Contents.Count != 0)
        {
            // Display weapon underlay
            visuals = CMHolsterVisuals.Full;
            // Get weapons size to accurately display storage visuals
            foreach (var item in ent.Comp.Contents)
            {
                if (TryComp(item, out ItemComponent? itemComp))
                    size += _item.GetItemShape(itemComp).GetArea();
            }
        }

        _appearance.SetData(ent, CMHolsterLayers.Fill, visuals);
        _appearance.SetData(ent, CMHolsterLayers.Size, size);
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

    private bool PickupSlot(EntityUid user, EntityUid holster)
    {
        if (!SlotCanInteract(user, holster, out var itemSlots))
            return false;

        foreach (var slot in itemSlots.Slots.Values.OrderBy(s => s.Priority))
        {
            var item = slot.ContainerSlot?.ContainedEntity;
            if (_itemSlots.TryEjectToHands(holster, slot, user, true))
            {
                if (item != null)
                    _adminLog.Add(LogType.RMCHolster, $"{ToPrettyString(user)} unholstered {ToPrettyString(item)}");

                return true;
            }
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
        var validSlots = new List<HolsterSlot>();

        var priority = 0;
        foreach (var flag in _quickEquipOrder)
        {
            var slots = _inventory.GetSlotEnumerator(user, flag);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity is not { } clothing)
                {
                    // If slot is empty and can equip
                    if (_inventory.CanEquip(user, item, slot.ID, out _))
                    {
                        validSlots.Add(new HolsterSlot(priority, false, slot, user, null));
                    }

                    continue;
                }

                // Check if the slot item has a CMHolsterComponent
                if (!TryComp(clothing, out CMHolsterComponent? holster))
                    continue;

                // Check if item matches holster whitelist (if it has one)
                // This is to prevent e.g. tools from being "holstered"
                if (holster.Whitelist is { } whitelist &&
                    !_whitelist.IsWhitelistPass(whitelist, item))
                    continue;

                // If holster has ItemSlotsComponent
                // Check if can be inserted into item slot
                if (HasComp<CMItemSlotsComponent>(clothing) &&
                    SlotCanInteract(user, clothing, out var slotComp) &&
                    TryGetAvailableSlot((clothing, slotComp),
                        item,
                        user,
                        out var itemSlot,
                        emptyOnly: true) &&
                    itemSlot.ContainerSlot != null)
                {
                    validSlots.Add(new HolsterSlot(priority, true, null, clothing, ItemSlot: itemSlot));
                    continue;
                }

                // If holster has StorageComponent
                // And item can be inserted
                if (HasComp<StorageComponent>(clothing) &&
                    _storage.CanInsert(clothing, item, out _))
                {
                    // TODO: Add storage holster to valid slots list
                    validSlots.Add(new HolsterSlot(priority, true, null, clothing, null));
                }
            }
            priority++;
        }

        validSlots.Sort();

        foreach (var slot in validSlots)
        {
            // Try equip to inventory slot
            if (!slot.IsHolster &&
                slot.Slot != null &&
                _inventory.TryEquip(user, item, slot.Slot.ID, true, checkDoafter: true))
                return;

            // Try insert into ItemSlot-based holster
            if (slot.ItemSlot != null &&
                _itemSlots.TryInsert(slot.Ent, slot.ItemSlot, item, user, excludeUserAudio: true))
            {
                _adminLog.Add(LogType.RMCHolster, $"{ToPrettyString(user)} holstered {ToPrettyString(item)}");
                return;
            }

            // Try insert into Storage-based holster
            if (slot.ItemSlot == null &&
                TryComp(slot.Ent, out StorageComponent? storage) &&
                TryComp(slot.Ent, out CMHolsterComponent? holster) &&
                !holster.Contents.Contains(item))
            {
                holster.Contents.Add(item);
                _hands.TryDrop(user, item);
                _storage.Insert(slot.Ent, item, out _, user, storage, playSound: false);
                _audio.PlayPredicted(holster.InsertSound, item, user);
                _adminLog.Add(LogType.RMCHolster, $"{ToPrettyString(user)} holstered {ToPrettyString(item)}");
                return;
            }
        }

        _popup.PopupClient(Loc.GetString("cm-inventory-unable-equip"), user, user, PopupType.SmallCaution);
    }

    private readonly record struct HolsterSlot(
        int Priority,
        bool IsHolster,
        ContainerSlot? Slot,
        EntityUid Ent,
        ItemSlot? ItemSlot) : IComparable<HolsterSlot>
    {
        public int CompareTo(HolsterSlot other)
        {
            // Sort holsters first
            // Then sort by priority between each holster and non-holster

            // If holster and holster
            // Sort by priority higher first
            if (IsHolster && other.IsHolster)
                return Priority.CompareTo(other.Priority);

            // If only first is holster, then sort it higher
            if (IsHolster)
                return -1;
            return 1;
        }
    }

    /// <summary>
    /// Tries to get any slot that the <paramref name="item"/> can be inserted into.
    /// </summary>
    /// <param name="ent">Entity that <paramref name="item"/> is being inserted into.</param>
    /// <param name="item">Entity being inserted into <paramref name="ent"/>.</param>
    /// <param name="userEnt">Entity inserting <paramref name="item"/> into <paramref name="ent"/>.</param>
    /// <param name="itemSlot">The ItemSlot on <paramref name="ent"/> to insert <paramref name="item"/> into.</param>
    /// <param name="emptyOnly"> True only returns slots that are empty.
    /// False returns any slot that is able to receive <paramref name="item"/>.</param>
    /// <returns>True when a slot is found. Otherwise, false.</returns>
    private bool TryGetAvailableSlot(Entity<ItemSlotsComponent?> ent,
        EntityUid item,
        Entity<HandsComponent?>? userEnt,
        [NotNullWhen(true)] out ItemSlot? itemSlot,
        bool emptyOnly = false)
    {
        // TODO Replace with ItemSlotsSystem version when upstream is merged
        itemSlot = null;

        if (userEnt is { } user && Resolve(user, ref user.Comp) && _hands.IsHolding(user, item))
        {
            if (!_hands.CanDrop(user, item, user.Comp))
                return false;
        }

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var slots = new List<ItemSlot>();
        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (emptyOnly && slot.ContainerSlot?.ContainedEntity != null)
                continue;

            if (_itemSlots.CanInsert(ent, item, userEnt, slot))
                slots.Add(slot);
        }

        if (slots.Count == 0)
            return false;

        slots.Sort(ItemSlotsSystem.SortEmpty);

        itemSlot = slots[0];
        return true;
    }

    // Get last item inserted into holster (can also be used to check if holster is empty)
    private bool TryGetLastInserted(Entity<CMHolsterComponent?> holster, out EntityUid item)
    {
        item = default;

        if (!Resolve(holster, ref holster.Comp))
            return false;

        var contents = holster.Comp.Contents;

        if (contents.Count == 0)
            return false;

        item = contents[contents.Count - 1];
        return true;
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
        if (TryComp(item, out CMHolsterComponent? holster))
        {
            if (holster.Cooldown is { } cooldown &&
                _timing.CurTime < holster.LastEjectAt + cooldown)
            {
                stop = true;
                _popup.PopupPredicted(holster.CooldownPopup, user, user, PopupType.SmallCaution);
                return false;
            }

            if (TryComp(item, out StorageComponent? storage) &&
                TryGetLastInserted((item, holster), out var weapon))
            {
                _hands.TryPickup(user, weapon);
                holster.Contents.Remove(weapon);
                _audio.PlayPredicted(holster.EjectSound, item, user);
                stop = true;
                return true;
            }

            if (PickupSlot(user, item))
            {
                _adminLog.Add(LogType.RMCHolster, $"{ToPrettyString(user)} unholstered {ToPrettyString(item)}");
                return true;
            }
        }

        var ev = new IsUnholsterableEvent();
        RaiseLocalEvent(item, ref ev);

        if (!ev.Unholsterable)
            return false;

        _adminLog.Add(LogType.RMCHolster, $"{ToPrettyString(user)} unholstered {ToPrettyString(item)}");
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
