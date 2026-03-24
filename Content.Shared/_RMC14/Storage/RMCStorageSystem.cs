using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Prototypes;
using Content.Shared._RMC14.Storage.Containers;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.ParaDrop;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using static Content.Shared.Storage.StorageComponent;

namespace Content.Shared._RMC14.Storage;

public sealed class RMCStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly List<EntityUid> _toRemove = new();
    private readonly List<EntityUid> _toClose = new();

    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<StorageComponent> _storageQuery;

    private readonly TimeSpan _stunStorage = TimeSpan.FromSeconds(4);

    public override void Initialize()
    {
        _itemQuery = GetEntityQuery<ItemComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();

        SubscribeLocalEvent<StorageComponent, CMStorageItemFillEvent>(OnStorageFillItem);

        SubscribeLocalEvent<StorageOpenDoAfterComponent, OpenStorageDoAfterEvent>(OnStorageOpenDoAfter);

        SubscribeLocalEvent<StorageSkillRequiredComponent, StorageInteractAttemptEvent>(OnStorageSkillOpenAttempt);
        SubscribeLocalEvent<StorageSkillRequiredComponent, DumpableDoAfterEvent>(OnDumpableDoAfter, before: [typeof(DumpableSystem)]);

        SubscribeLocalEvent<StorageCloseOnMoveComponent, GotEquippedEvent>(OnStorageEquip);

        SubscribeLocalEvent<BlockEntityStorageComponent, InsertIntoEntityStorageAttemptEvent>(OnBlockInsertIntoEntityStorageAttempt);

        SubscribeLocalEvent<MarineComponent, EntGotRemovedFromContainerMessage>(OnRemovedMarineFromContainer);

        SubscribeLocalEvent<StorageNestedOpenSkillRequiredComponent, StorageInteractAttemptEvent>(OnNestedSkillRequiredInteractAttempt);

        SubscribeLocalEvent<SkyFallingComponent, StorageInteractAttemptEvent>(OnSkyFalling);
        SubscribeLocalEvent<CrashLandingComponent, StorageInteractAttemptEvent>(OnCrashLanding);
        SubscribeLocalEvent<ParaDroppingComponent, StorageInteractAttemptEvent>(OnParaDropping);

        SubscribeLocalEvent<RMCEntityStorageWhitelistComponent, ContainerIsInsertingAttemptEvent>(OnEntityStorageWhitelistAttempt);

        SubscribeLocalEvent<EntityStorageCloseOnMapInitComponent, MapInitEvent>(OnEntityStorageClose);

        SubscribeLocalEvent<RMCContainerEmptyOnDestructionComponent, DestructionEventArgs>(OnContainerEmptyDestroyed);
        SubscribeLocalEvent<RMCContainerEmptyOnDestructionComponent, EntityTerminatingEvent>(OnContainerEmptyDeleted);

        SubscribeLocalEvent<OpenStorageOnGearEquipComponent, StartingGearEquippedEvent>(OnOpenStorageStartingGear);

        Subs.BuiEvents<StorageCloseOnMoveComponent>(StorageUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnCloseOnMoveUIOpened);
        });

        Subs.BuiEvents<StorageOpenComponent>(StorageUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnCloseOnMoveUIClosed);
        });

        UpdatesAfter.Add(typeof(SharedStorageSystem));
    }

    private void OnDumpableDoAfter(Entity<StorageSkillRequiredComponent> ent, ref DumpableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (TryCancel(args.User, ent))
            args.Handled = true;
    }

    private void OnStorageFillItem(Entity<StorageComponent> storage, ref CMStorageItemFillEvent args)
    {
        var tries = 0;
        // Ignore stackables because SharedStorageSystem.OnAttemptInsert does not stack items.
        while (!_storage.CanInsert(storage, args.Item, null, out var reason, ignoreStacks: true) &&
               reason == "comp-storage-insufficient-capacity" &&
               tries < 3)
        {
            tries++;

            // TODO RMC14 make this error if this is a cm-specific storage
            if (CMPrototypeExtensions.FilterCM)
                Log.Warning($"Storage {ToPrettyString(storage)} can't fit {ToPrettyString(args.Item)}");

            foreach (var shape in _item.GetItemShape((storage, args.Storage), (args.Item, args.Item)))
            {
                var grid = args.Storage.Grid;
                if (grid.Count == 0)
                {
                    grid.Add(shape);
                    continue;
                }

                // TODO RMC14 this might create more space than is necessary to fit the item if there is some free space left in the storage before expanding it
                var last = grid[^1];
                var expanded = new Box2i(last.Left, last.Bottom, last.Right + shape.Width + 1, last.Top);

                if (expanded.Top < shape.Top)
                    expanded.Top = shape.Top;

                grid[^1] = expanded;
            }
        }
    }

    public bool IgnoreItemSize(Entity<StorageComponent> storage, EntityUid item)
    {
        return TryComp(storage, out IgnoreContentsSizeComponent? ignore) &&
               _entityWhitelist.IsValid(ignore.Items, item);
    }

    public bool OpenDoAfter(EntityUid uid, EntityUid entity, StorageComponent? storageComp = null, bool silent = false)
    {
        if (!TryComp(uid, out StorageOpenDoAfterComponent? comp) ||
            comp.Duration == TimeSpan.Zero)
        {
            return false;
        }

        if (comp.SkipInHand && _hands.IsHolding(entity, uid))
            return false;

        if (comp.SkipOnGround && !_inventory.TryGetContainingSlot(uid, out var _))
            return false;

        var ev = new OpenStorageDoAfterEvent(GetNetEntity(uid), GetNetEntity(entity), silent);
        var doAfter = new DoAfterArgs(EntityManager, entity, comp.Duration, ev, uid)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfter);

        return true;
    }

    private void OnStorageOpenDoAfter(Entity<StorageOpenDoAfterComponent> ent, ref OpenStorageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryGetEntity(args.Uid, out var uid) ||
            !TryGetEntity(args.Entity, out var entity))
        {
            return;
        }

        if (!TryComp(uid, out StorageComponent? storage))
            return;

        args.Handled = true;
        _storage.OpenStorageUI(uid.Value, entity.Value, storage, args.Silent, false);
    }

    private void OnStorageSkillOpenAttempt(Entity<StorageSkillRequiredComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryCancel(args.User, ent))
            args.Cancelled = true;
    }

    private void OnStorageEquip(Entity<StorageCloseOnMoveComponent> ent, ref GotEquippedEvent args)
    {
        _ui.CloseUi(ent.Owner, StorageUiKey.Key, args.Equipee);
        if (TryComp<StorageOpenComponent>(ent, out var comp))
            comp.OpenedAt.Remove(args.Equipee);
    }

    private void OnBlockInsertIntoEntityStorageAttempt(Entity<BlockEntityStorageComponent> ent, ref InsertIntoEntityStorageAttemptEvent args)
    {
        if (_entityWhitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Container))
            args.Cancelled = true;
    }

    private void OnRemovedMarineFromContainer(Entity<MarineComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (TerminatingOrDeleted(ent))
            return;

        if (!HasComp<NoStunOnExitComponent>(args.Container.Owner))
            _stun.TryStun(ent, _stunStorage, true);

        if (HasComp<SkyFallingComponent>(args.Container.Owner) || HasComp<CrashLandingComponent>(args.Container.Owner))
        {
            var ev = new AttemptCrashLandEvent();
            RaiseLocalEvent(ent, ref ev);

            if (!ev.Cancelled)
                _crashLand.TryCrashLand(ent.Owner, true);
        }
    }

    private void OnNestedSkillRequiredInteractAttempt(Entity<StorageNestedOpenSkillRequiredComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (!_container.TryGetContainingContainer((ent, null), out var container) ||
            !TryComp(container.Owner, out StorageComponent? parentStorage) ||
            !parentStorage.StoredItems.ContainsKey(ent))
        {
            return;
        }

        if (_skills.HasSkills(args.User, ent.Comp.Skills))
            return;

        args.Cancelled = true;
        if (args.Silent)
            return;

        var msg = Loc.GetString("rmc-storage-nested-unable", ("nested", ent), ("parent", container.Owner));
        _popup.PopupClient(msg, ent, args.User, PopupType.SmallCaution);
    }

    private void OnEntityStorageWhitelistAttempt(Entity<RMCEntityStorageWhitelistComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_entityWhitelist.IsWhitelistPass(ent.Comp.Whitelist, args.EntityUid))
            args.Cancel();
    }

    private void OnEntityStorageClose(Entity<EntityStorageCloseOnMapInitComponent> ent, ref MapInitEvent args)
    {
        _toClose.Add(ent);
    }

    private void OnCloseOnMoveUIOpened(Entity<StorageCloseOnMoveComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.SkipInHand && _hands.IsHolding(args.Actor, ent))
            return;

        var user = args.Actor;
        var coordinates = GetNetCoordinates(_transform.GetMoverCoordinates(user));
        EnsureComp<StorageOpenComponent>(ent).OpenedAt[user] = coordinates;
    }

    private void OnCloseOnMoveUIClosed(Entity<StorageOpenComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.OpenedAt.Remove(args.Actor);
    }

    private void OnSkyFalling(Entity<SkyFallingComponent> ent, ref StorageInteractAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnParaDropping(Entity<ParaDroppingComponent> ent, ref StorageInteractAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnCrashLanding(Entity<CrashLandingComponent> ent, ref StorageInteractAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private bool TryCancel(EntityUid user, Entity<StorageSkillRequiredComponent> storage)
    {
        if (!_skills.HasAllSkills(user, storage.Comp.Skills))
        {
            _popup.PopupClient(Loc.GetString("cm-storage-unskilled"), storage, user, PopupType.SmallCaution);
            return true;
        }

        return false;
    }

    private bool CanInsertStorageLimit(Entity<StorageComponent?, LimitedStorageComponent?> limited, EntityUid toInsert, out LocId popup)
    {
        popup = default;
        if (!Resolve(limited, ref limited.Comp2, false) ||
            !_storageQuery.Resolve(limited, ref limited.Comp1, false))
        {
            return true;
        }

        foreach (var limit in limited.Comp2.Limits)
        {
            if (!_entityWhitelist.IsWhitelistPassOrNull(limit.Whitelist, toInsert))
                continue;

            if (_entityWhitelist.IsBlacklistPass(limit.Blacklist, toInsert))
                continue;

            var storedCount = 0;
            foreach (var stored in limited.Comp1.StoredItems.Keys)
            {
                if (stored == toInsert)
                    continue;

                if (!_entityWhitelist.IsWhitelistPassOrNull(limit.Whitelist, stored))
                    continue;

                if (_entityWhitelist.IsBlacklistPass(limit.Blacklist, stored))
                    continue;

                storedCount++;
                if (storedCount >= limit.Count)
                    break;
            }

            if (storedCount < limit.Count)
                continue;

            popup = limit.Popup == default ? "rmc-storage-limit-cant-fit" : limit.Popup;
            return false;
        }

        return true;
    }

    public bool CanInsertStoreSkill(Entity<StorageComponent?, StorageStoreSkillRequiredComponent?> store, EntityUid toInsert, EntityUid? user, out LocId popup)
    {
        popup = default;
        if (user == null)
            return true;

        if (!Resolve(store, ref store.Comp2, false) ||
            !_storageQuery.Resolve(store, ref store.Comp1, false))
        {
            return true;
        }

        foreach (var entry in store.Comp2.Entries)
        {
            if (_entityWhitelist.IsWhitelistFail(entry.Whitelist, toInsert))
                continue;

            if (_skills.HasSkills(user.Value, entry.Skills))
                continue;

            popup = "rmc-storage-store-skill-unable";
            return false;
        }

        return true;
    }

    private bool CanEjectStoreSkill(Entity<StorageComponent?, StorageSkillRequiredComponent?> store, EntityUid? user, out LocId popup)
    {
        popup = default;
        if (user == null)
            return true;

        if (!Resolve(store, ref store.Comp2, false) ||
            !_storageQuery.Resolve(store, ref store.Comp1, false))
        {
            return true;
        }

        if (!_skills.HasAllSkills(user.Value, store.Comp2.Skills))
        {
            popup = Loc.GetString("cm-storage-unskilled");
            return false;
        }

        return true;
    }

    public bool TryGetLastItem(Entity<StorageComponent?> storage, out EntityUid item)
    {
        item = default;
        if (!Resolve(storage, ref storage.Comp, false))
            return false;

        ItemStorageLocation? lastLocation = null;
        foreach (var (stored, location) in storage.Comp.StoredItems)
        {
            if (lastLocation is not { } last ||
                last.Position.Y < location.Position.Y)
            {
                item = stored;
                lastLocation = location;
                continue;
            }

            if (last.Position.Y == location.Position.Y &&
                last.Position.X > location.Position.X)
            {
                item = stored;
                lastLocation = location;
            }
        }

        return item != default;
    }

    public bool TryGetFirstItem(Entity<StorageComponent?> storage, out EntityUid item)
    {
        item = default;
        if (!Resolve(storage, ref storage.Comp, false))
            return false;

        ItemStorageLocation? firstLocation = null;
        foreach (var (stored, location) in storage.Comp.StoredItems)
        {
            if (firstLocation is not { } first ||
                first.Position.Y > location.Position.Y)
            {
                item = stored;
                firstLocation = location;
                continue;
            }

            if (first.Position.Y == location.Position.Y &&
                first.Position.X < location.Position.X)
            {
                item = stored;
                firstLocation = location;
            }
        }

        return item != default;
    }

    public bool CanInsert(Entity<StorageComponent?> storage, EntityUid toInsert, EntityUid? user, out LocId popup)
    {
        if (!CanInsertStorageLimit((storage, storage, null), toInsert, out popup))
            return false;

        if (!CanInsertStoreSkill((storage, storage, null), toInsert, user, out popup))
            return false;

        return true;
    }

    public bool CanEject(EntityUid storage, EntityUid user, out LocId popup)
    {
        if (!CanEjectStoreSkill(storage, user, out popup))
            return false;

        return true;
    }

    private void OnContainerEmptyDestroyed(Entity<RMCContainerEmptyOnDestructionComponent> containerEnt, ref DestructionEventArgs args)
    {
        if (!containerEnt.Comp.OnDestruction)
            return;

        ContainerDestructionEmpty(containerEnt);
    }

    private void OnContainerEmptyDeleted(Entity<RMCContainerEmptyOnDestructionComponent> containerEnt, ref EntityTerminatingEvent args)
    {
        if (!containerEnt.Comp.OnDelete)
            return;

        ContainerDestructionEmpty(containerEnt);
    }

    private void ContainerDestructionEmpty(Entity<RMCContainerEmptyOnDestructionComponent> containerEnt)
    {
        if (!TryComp(containerEnt, out TransformComponent? transform) || TerminatingOrDeleted(transform.GridUid) || !Exists(containerEnt) || !TryComp(containerEnt, out ContainerManagerComponent? containerManager))
            return;

        var ev = new RMCContainerDestructionEmptyEvent();
        RaiseLocalEvent(containerEnt, ref ev);

        if (ev.Handled)
            return;

        var containers = _container.GetAllContainers(containerEnt, containerManager);
        foreach (var contain in containers)
        {
            _container.EmptyContainer(contain);
        }
    }

    public int EstimateFreeColumns(Entity<StorageComponent?> storage)
    {
        if (!Resolve(storage, ref storage.Comp, false))
            return 0;

        // Since RMC14 storage is simplified we can make assumptions about how
        // items are laid out and not allocate 100 lists just to check this
        var columns = 0;
        foreach (var grid in storage.Comp.Grid)
        {
            columns += (grid.Width + 1) * (grid.Height + 1 / 2);
        }

        foreach (var (item, _) in storage.Comp.StoredItems)
        {
            if (!_itemQuery.TryComp(item, out var itemComp))
                continue;

            var shapes = _item.GetItemShape(storage, (item, itemComp));
            if (shapes.Count == 0)
                continue;

            var shape = shapes[0];
            columns -= shape.Width;
        }

        return columns;
    }

    private void OnOpenStorageStartingGear(Entity<OpenStorageOnGearEquipComponent> ent, ref StartingGearEquippedEvent args)
    {
        var slots = _inventory.GetSlotEnumerator(ent.Owner);
        while (slots.MoveNext(out var slot))
        {
            if (!_storageQuery.TryComp(slot.ContainedEntity, out var storageComp))
                continue;

            if (!_entityWhitelist.CheckBoth(slot.ContainedEntity, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;

            _storage.OpenStorageUI(slot.ContainedEntity.Value, ent.Owner, storageComp, doAfter: false);
        }
    }

    public override void Update(float frameTime)
    {
        try
        {
            if (_net.IsServer)
            {
                foreach (var toClose in _toClose)
                {
                    var locked = _lock.IsLocked(toClose);
                    if (locked)
                        _lock.Unlock(toClose, null);

                    _entityStorage.OpenStorage(toClose);
                    _entityStorage.CloseStorage(toClose);

                    if (locked)
                        _lock.Lock(toClose, null);
                }
            }
        }
        finally
        {
            _toClose.Clear();
        }

        var removeOnlyQuery = EntityQueryEnumerator<RemoveOnlyStorageComponent>();
        while (removeOnlyQuery.MoveNext(out var uid, out var comp))
        {
            if (TryComp(uid, out StorageComponent? storage))
            {
                if (comp.Blacklist != null)
                    storage.Blacklist = comp.Blacklist;

                storage.Whitelist = comp.Whitelist;
                Dirty(uid, storage);
            }

            RemCompDeferred<RemoveOnlyStorageComponent>(uid);
        }

        var openQuery = EntityQueryEnumerator<StorageOpenComponent>();
        while (openQuery.MoveNext(out var uid, out var open))
        {
            _toRemove.Clear();
            foreach (var (user, netOrigin) in open.OpenedAt)
            {
                if (TerminatingOrDeleted(user))
                {
                    _toRemove.Add(user);
                    continue;
                }

                var origin = GetCoordinates(netOrigin);
                var current = _transform.GetMoverCoordinates(user);

                if (!_transform.InRange(origin, current, 0.1f))
                    _toRemove.Add(user);
            }

            foreach (var user in _toRemove)
            {
                _ui.CloseUi(uid, StorageUiKey.Key, user);
                open.OpenedAt.Remove(user);
            }

            if (open.OpenedAt.Count == 0)
                RemCompDeferred<StorageOpenComponent>(uid);
        }
    }
}
