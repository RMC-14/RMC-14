using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Supply;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Vehicle;

public sealed class VehicleSupplySystem : EntitySystem
{
    private readonly record struct HardpointItemInfo(string ProtoId, HashSet<ProtoId<TagPrototype>> Tags);
    private const int VendedHardpointAmmoCount = 3;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _vendor = default!;
    [Dependency] private readonly VehicleSystem _rmcVehicles = default!;

    private readonly Dictionary<string, List<HardpointItemInfo>> _hardpointItemsByType = new();
    private readonly Dictionary<string, string> _hardpointTypeByProto = new();
    private readonly Dictionary<string, List<string>> _hardpointsByVehicleCache = new();

    private readonly record struct PreviewOffset(
        Vector2 Base,
        bool UseDirectional,
        Vector2 North,
        Vector2 East,
        Vector2 South,
        Vector2 West);
    private readonly record struct VendorHardpointEntry(
        string Id,
        string SharedKey,
        int SortOrder,
        string DisplayName,
        string SectionName,
        int SectionOrder);

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<VehicleSupplyConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpen);
        SubscribeLocalEvent<VehicleHardpointVendorComponent, MapInitEvent>(OnVendorMapInit);
        SubscribeLocalEvent<VehicleHardpointVendorComponent, BeforeActivatableUIOpenEvent>(OnVendorBeforeUiOpen);
        SubscribeLocalEvent<VehicleSupplyLiftComponent, MapInitEvent>(OnLiftMapInit);
        SubscribeLocalEvent<ActorComponent, RMCAutomatedVendedUserEvent>(OnAutomatedVendorVended);

        Subs.BuiEvents<VehicleSupplyConsoleComponent>(VehicleSupplyUIKey.Key, subs =>
        {
            subs.Event<VehicleSupplySelectMsg>(OnVehicleSelected);
            subs.Event<VehicleSupplyLiftMsg>(OnLiftToggleRequested);
        });

        SubscribeLocalEvent<TechUnlockVehicleEvent>(OnTechUnlockVehicle);

        ReloadHardpointItems();
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static int GetStoredCount(VehicleSupplyLiftComponent lift, string key)
    {
        return lift.Stored.TryGetValue(key, out var count) ? count : 0;
    }

    private static int GetVendorAvailableVehicleCount(VehicleSupplyLiftComponent lift, string key)
    {
        var count = GetStoredCount(lift, key);

        if (lift.Deployed.Contains(key))
            count++;

        if (!string.IsNullOrWhiteSpace(lift.PendingVehicle) &&
            Normalize(lift.PendingVehicle) == key)
        {
            count++;
        }

        return count;
    }

    private static void AddStored(VehicleSupplyLiftComponent lift, string key, int amount = 1)
    {
        if (amount <= 0)
            return;

        lift.Stored[key] = GetStoredCount(lift, key) + amount;
    }

    private static bool TryRemoveStored(VehicleSupplyLiftComponent lift, string key, int amount = 1)
    {
        if (amount <= 0)
            return true;

        if (!lift.Stored.TryGetValue(key, out var count) || count < amount)
            return false;

        var next = count - amount;
        if (next <= 0)
            lift.Stored.Remove(key);
        else
            lift.Stored[key] = next;

        return true;
    }

    private static void AddStoredEntity(VehicleSupplyLiftComponent lift, string key, EntityUid vehicle)
    {
        if (!lift.StoredEntities.TryGetValue(key, out var list))
        {
            list = new List<EntityUid>();
            lift.StoredEntities[key] = list;
        }

        list.Add(vehicle);
    }

    private bool TryPopStoredEntity(VehicleSupplyLiftComponent lift, string key, out EntityUid vehicle)
    {
        vehicle = default;
        if (!lift.StoredEntities.TryGetValue(key, out var list))
            return false;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var candidate = list[i];
            list.RemoveAt(i);
            if (Deleted(candidate))
                continue;

            if (list.Count == 0)
                lift.StoredEntities.Remove(key);

            vehicle = candidate;
            return true;
        }

        if (list.Count == 0)
            lift.StoredEntities.Remove(key);

        return false;
    }

    private bool TryTakeStoredEntity(VehicleSupplyLiftComponent lift, string key, int index, out EntityUid vehicle)
    {
        vehicle = default;
        if (!lift.StoredEntities.TryGetValue(key, out var list) || list.Count == 0)
            return false;

        if (index < 0 || index >= list.Count)
            index = list.Count - 1;

        for (var attempts = 0; attempts < list.Count; attempts++)
        {
            var takeIndex = index;
            var candidate = list[takeIndex];
            list.RemoveAt(takeIndex);

            if (Deleted(candidate))
            {
                if (list.Count == 0)
                    break;

                index = Math.Min(index, list.Count - 1);
                continue;
            }

            if (list.Count == 0)
                lift.StoredEntities.Remove(key);

            vehicle = candidate;
            return true;
        }

        if (list.Count == 0)
            lift.StoredEntities.Remove(key);

        return false;
    }

    private bool TryGetStoredEntity(VehicleSupplyLiftComponent lift, string key, int index, out EntityUid vehicle)
    {
        vehicle = default;
        if (!lift.StoredEntities.TryGetValue(key, out var list) || list.Count == 0)
            return false;

        if (index < 0 || index >= list.Count)
            return false;

        var candidate = list[index];
        if (!Deleted(candidate))
        {
            vehicle = candidate;
            return true;
        }

        list.RemoveAt(index);

        if (list.Count == 0)
            lift.StoredEntities.Remove(key);

        return false;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        ReloadHardpointItems();
        _hardpointsByVehicleCache.Clear();
    }

    private void ReloadHardpointItems()
    {
        _hardpointItemsByType.Clear();
        _hardpointTypeByProto.Clear();

        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (!proto.TryGetComponent(out HardpointItemComponent? hardpointItem, _compFactory))
                continue;

            _hardpointTypeByProto[Normalize(proto.ID)] = hardpointItem.HardpointType;

            var key = Normalize(hardpointItem.HardpointType);
            if (!_hardpointItemsByType.TryGetValue(key, out var list))
            {
                list = new List<HardpointItemInfo>();
                _hardpointItemsByType[key] = list;
            }

            var tags = new HashSet<ProtoId<TagPrototype>>();
            if (proto.TryGetComponent(out TagComponent? tagComp, _compFactory))
                tags = new HashSet<ProtoId<TagPrototype>>(tagComp.Tags);

            list.Add(new HardpointItemInfo(proto.ID, tags));
        }
    }

    private void OnTechUnlockVehicle(TechUnlockVehicleEvent ev)
    {
        if (string.IsNullOrWhiteSpace(ev.Unlock))
            return;

        var tech = EnsureSupplyTech();
        var unlock = Normalize(ev.Unlock);
        if (!tech.Comp.Unlocked.Contains(unlock))
        {
            tech.Comp.Unlocked.Add(unlock);
            Dirty(tech);
        }

        var liftQuery = EntityQueryEnumerator<VehicleSupplyLiftComponent>();
        while (liftQuery.MoveNext(out var uid, out var lift))
        {
            if (GetStoredCount(lift, unlock) > 0 || lift.Deployed.Contains(unlock))
                continue;

            AddStored(lift, unlock);
            Dirty(uid, lift);
        }

        SendConsoleStateAll();
        UpdateVendorSectionsAll();
    }

    private void OnConsoleBeforeUiOpen(Entity<VehicleSupplyConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SendConsoleState(ent.Owner, ent.Comp);
    }

    private void OnLiftMapInit(Entity<VehicleSupplyLiftComponent> ent, ref MapInitEvent args)
    {
        SeedStoredFromConsoles(ent);

        Dirty(ent);
    }

    private void SeedStoredFromConsoles(Entity<VehicleSupplyLiftComponent> lift)
    {
        var unlocked = BuildUnlockedSet();
        var mapId = _transform.GetMapId(lift.Owner);

        var query = EntityQueryEnumerator<VehicleSupplyConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var console, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            foreach (var entry in console.Vehicles)
            {
                if (!IsEntryUnlocked(entry, unlocked))
                    continue;

                var key = Normalize(entry.Vehicle.Id);
                if (lift.Comp.Deployed.Contains(key))
                    continue;

                if (GetStoredCount(lift.Comp, key) > 0)
                    continue;

                AddStored(lift.Comp, key);
            }
        }
    }

    private void OnVendorBeforeUiOpen(Entity<VehicleHardpointVendorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateVendorSections(ent.Owner, ent.Comp);
    }

    private void OnVendorMapInit(Entity<VehicleHardpointVendorComponent> ent, ref MapInitEvent args)
    {
        UpdateVendorSections(ent.Owner, ent.Comp);
    }

    private void OnAutomatedVendorVended(Entity<ActorComponent> ent, ref RMCAutomatedVendedUserEvent args)
    {
        if (!HasComp<HardpointItemComponent>(args.Item))
            return;

        TrySpawnVendedHardpointAmmo(ent.Owner, args.Item);
        UpdateVendorSectionsAll();
    }

    private void TrySpawnVendedHardpointAmmo(EntityUid user, EntityUid hardpointItem)
    {
        if (!TryComp(hardpointItem, out VehicleHardpointAmmoComponent? _) ||
            !TryComp(hardpointItem, out RefillableByBulletBoxComponent? refillable) ||
            refillable.BulletType is not { } bulletType)
        {
            return;
        }

        if (!TryResolveVendedAmmoPrototype(bulletType, out var ammoProto))
            return;

        for (var i = 0; i < VendedHardpointAmmoCount; i++)
        {
            SpawnNextToOrDrop(ammoProto, user);
        }
    }

    private bool TryResolveVendedAmmoPrototype(EntProtoId bulletType, out EntProtoId ammoProto)
    {
        ammoProto = bulletType;

        if (_prototypes.TryIndex<EntityPrototype>(bulletType, out var exact) && !exact.Abstract)
            return true;

        string? fallback = null;
        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (!proto.TryGetComponent(out BulletBoxComponent? box, _compFactory))
                continue;

            if (box.BulletType != bulletType)
                continue;

            if (fallback == null || string.CompareOrdinal(proto.ID, fallback) < 0)
                fallback = proto.ID;
        }

        if (fallback == null)
            return false;

        ammoProto = fallback;
        return true;
    }

    private void OnVehicleSelected(Entity<VehicleSupplyConsoleComponent> ent, ref VehicleSupplySelectMsg args)
    {
        if (string.IsNullOrWhiteSpace(args.VehicleId))
            return;

        if (!TryGetLift(ent.Owner, ent.Comp, out var lift))
            return;

        if (!TryGetEntry(ent.Comp, args.VehicleId, out var entry))
            return;

        var unlocked = BuildUnlockedSet();
        if (!IsEntryUnlocked(entry, unlocked))
            return;

        var id = entry.Vehicle.Id;
        var idKey = Normalize(id);
        if (Normalize(lift.Comp.PendingVehicle) == idKey)
            return;

        if (GetStoredCount(lift.Comp, idKey) <= 0)
            return;

        ent.Comp.SelectedVehicle = id;
        ent.Comp.SelectedVehicleCopyIndex = Math.Max(0, args.CopyIndex);
        SendConsoleStateAll();
    }

    private void OnLiftToggleRequested(Entity<VehicleSupplyConsoleComponent> ent, ref VehicleSupplyLiftMsg args)
    {
        if (!TryGetLift(ent.Owner, ent.Comp, out var lift))
            return;

        TryToggleLift(ent, lift, args.Raise);
    }

    private void TryToggleLift(Entity<VehicleSupplyConsoleComponent> console, Entity<VehicleSupplyLiftComponent> lift, bool raise)
    {
        var comp = lift.Comp;
        if (comp.NextMode != null || comp.Busy)
            return;

        if (comp.Mode == VehicleSupplyLiftMode.Lowering || comp.Mode == VehicleSupplyLiftMode.Raising)
            return;

        if (raise)
        {
            if (comp.Mode == VehicleSupplyLiftMode.Raised)
                return;
            var selected = console.Comp.SelectedVehicle;
            var canQueueVehicle = false;
            string? nextVehicle = null;

            if (!string.IsNullOrWhiteSpace(selected))
            {
                if (TryGetEntry(console.Comp, selected, out var entry))
                {
                    var unlocked = BuildUnlockedSet();
                    if (IsEntryUnlocked(entry, unlocked))
                    {
                        var key = Normalize(selected);
                        if (GetStoredCount(comp, key) > 0 && _prototypes.TryIndex<EntityPrototype>(selected, out _))
                        {
                            if (TryRemoveStored(comp, key))
                            {
                                canQueueVehicle = true;
                                nextVehicle = selected;
                                comp.PendingVehicleEntity = null;
                                if (TryTakeStoredEntity(comp, key, console.Comp.SelectedVehicleCopyIndex, out var pendingEntity))
                                    comp.PendingVehicleEntity = pendingEntity;

                                console.Comp.SelectedVehicle = string.Empty;
                                console.Comp.SelectedVehicleCopyIndex = 0;
                            }
                        }
                    }
                }
            }

            if (canQueueVehicle && nextVehicle != null)
            {
                comp.PendingVehicle = nextVehicle;
            }
            else
            {
                comp.PendingVehicle = string.Empty;
                comp.PendingVehicleEntity = null;
            }

            UpdateVendorSectionsAll();
        }
        else
        {
            if (comp.Mode == VehicleSupplyLiftMode.Lowered)
                return;

            if (IsLoweringBlocked(lift))
                return;
        }

        comp.ToggledAt = _timing.CurTime;
        comp.Busy = true;
        SetMode(lift, VehicleSupplyLiftMode.Preparing, raise ? VehicleSupplyLiftMode.Raising : VehicleSupplyLiftMode.Lowering);
    }

    private bool IsLoweringBlocked(Entity<VehicleSupplyLiftComponent> lift)
    {
        if (lift.Comp.ActiveVehicle is { } active &&
            IsOnLift(lift, active) &&
            _rmcVehicles.TryGetInteriorMapId(active, out var interiorMap))
        {
            var actorQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
            while (actorQuery.MoveNext(out _, out _, out var xform))
            {
                if (xform.MapID == interiorMap)
                    return true;
            }
        }

        var mask = (int) (CollisionGroup.MobLayer | CollisionGroup.MobMask);
        foreach (var entity in _physics.GetEntitiesIntersectingBody(lift, mask, false))
        {
            if (HasComp<MobStateComponent>(entity))
                return true;
        }

        return false;
    }

    private void SetMode(Entity<VehicleSupplyLiftComponent> lift, VehicleSupplyLiftMode mode, VehicleSupplyLiftMode? nextMode)
    {
        lift.Comp.Mode = mode;
        lift.Comp.NextMode = nextMode;
        Dirty(lift);
        SendConsoleStateAll();
    }

    private void TryPlayAudio(Entity<VehicleSupplyLiftComponent> lift)
    {
        var comp = lift.Comp;
        if (comp.Audio != null || comp.ToggledAt == null)
            return;

        var time = _timing.CurTime;
        if (comp.NextMode == VehicleSupplyLiftMode.Lowering || comp.Mode == VehicleSupplyLiftMode.Lowering)
        {
            if (time < comp.ToggledAt + comp.LowerSoundDelay)
                return;

            comp.Audio = _audio.PlayPvs(comp.LoweringSound, lift)?.Entity;
            return;
        }

        if (comp.NextMode == VehicleSupplyLiftMode.Raising || comp.Mode == VehicleSupplyLiftMode.Raising)
        {
            if (time < comp.ToggledAt + comp.RaiseSoundDelay)
                return;

            comp.Audio = _audio.PlayPvs(comp.RaisingSound, lift)?.Entity;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var updateUi = false;
        var liftQuery = EntityQueryEnumerator<VehicleSupplyLiftComponent>();
        while (liftQuery.MoveNext(out var uid, out var lift))
        {
            if (CleanupDestroyedActive((uid, lift)))
                updateUi = true;

            if (ProcessLift((uid, lift)))
                updateUi = true;
        }

        if (updateUi)
            SendConsoleStateAll();
    }

    private bool CleanupDestroyedActive(Entity<VehicleSupplyLiftComponent> lift)
    {
        var comp = lift.Comp;
        if (comp.ActiveVehicle == null)
            return false;

        var active = comp.ActiveVehicle.Value;
        if (Deleted(active))
        {
            if (!string.IsNullOrWhiteSpace(comp.ActiveVehicleId))
                comp.Deployed.Remove(Normalize(comp.ActiveVehicleId));

            comp.ActiveVehicle = null;
            comp.ActiveVehicleId = string.Empty;
            return true;
        }

        return false;
    }

    private bool ProcessLift(Entity<VehicleSupplyLiftComponent> lift)
    {
        var comp = lift.Comp;
        if (comp.ToggledAt == null)
            return false;

        var time = _timing.CurTime;
        if (time > comp.ToggledAt + comp.ToggleDelay)
        {
            comp.ToggledAt = null;
            comp.Busy = false;
            Dirty(lift);
            return true;
        }

        TryPlayAudio(lift);

        var delay = comp.NextMode == VehicleSupplyLiftMode.Raising ? comp.RaiseDelay : comp.LowerDelay;
        if (comp.Mode == VehicleSupplyLiftMode.Preparing &&
            comp.NextMode != null &&
            time > comp.ToggledAt + delay)
        {
            SetMode(lift, comp.NextMode.Value, null);
            return true;
        }

        if (comp.Mode != VehicleSupplyLiftMode.Lowering && comp.Mode != VehicleSupplyLiftMode.Raising)
            return false;

        var moveDelay = delay + (comp.Mode == VehicleSupplyLiftMode.Raising ? comp.RaiseDelay : comp.LowerDelay);
        if (time > comp.ToggledAt + moveDelay)
        {
            comp.Audio = null;

            var mode = comp.Mode == VehicleSupplyLiftMode.Raising
                ? VehicleSupplyLiftMode.Raised
                : VehicleSupplyLiftMode.Lowered;

            SetMode(lift, mode, comp.NextMode);
            if (mode == VehicleSupplyLiftMode.Raised)
                SpawnVehicle(lift);
            else
                StoreVehicle(lift);

            comp.ToggledAt = null;
            comp.Busy = false;
            Dirty(lift);
            return true;
        }

        return false;
    }

    private void SpawnVehicle(Entity<VehicleSupplyLiftComponent> lift)
    {
        var comp = lift.Comp;
        var pending = comp.PendingVehicle;
        if (string.IsNullOrWhiteSpace(pending))
            return;

        var key = Normalize(pending);
        if (comp.PendingVehicleEntity is { } pendingEntity && Exists(pendingEntity))
        {
            var moverCoords = _transform.GetMoverCoordinates(lift);
            var mapCoords = _transform.ToMapCoordinates(moverCoords);
            _transform.SetMapCoordinates(pendingEntity, mapCoords);

            comp.ActiveVehicle = pendingEntity;
            comp.ActiveVehicleId = pending;
            comp.PendingVehicle = string.Empty;
            comp.PendingVehicleEntity = null;
            comp.Deployed.Add(key);
            return;
        }

        comp.PendingVehicleEntity = null;
        if (TryPopStoredEntity(comp, key, out var stored))
        {
            var moverCoords = _transform.GetMoverCoordinates(lift);
            var mapCoords = _transform.ToMapCoordinates(moverCoords);
            _transform.SetMapCoordinates(stored, mapCoords);

            comp.ActiveVehicle = stored;
            comp.ActiveVehicleId = pending;
            comp.PendingVehicle = string.Empty;
            comp.Deployed.Add(key);
            return;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(pending, out _))
        {
            AddStored(comp, key);
            comp.PendingVehicle = string.Empty;
            UpdateVendorSectionsAll();
            return;
        }

        var spawnCoords = _transform.GetMoverCoordinates(lift);
        var vehicle = SpawnAtPosition(pending, spawnCoords);

        comp.ActiveVehicle = vehicle;
        comp.ActiveVehicleId = pending;
        comp.PendingVehicle = string.Empty;
        comp.Deployed.Add(key);
    }

    private void StoreVehicle(Entity<VehicleSupplyLiftComponent> lift)
    {
        var comp = lift.Comp;
        if (comp.ActiveVehicle == null)
            return;

        var active = comp.ActiveVehicle.Value;
        if (!IsOnLift(lift, active))
            return;

        if (!string.IsNullOrWhiteSpace(comp.ActiveVehicleId))
        {
            var key = Normalize(comp.ActiveVehicleId);
            comp.Deployed.Remove(key);
            AddStored(comp, key);
            AddStoredEntity(comp, key, active);
        }

        _transform.SetParent(active, EntityUid.Invalid);
        comp.ActiveVehicle = null;
        comp.ActiveVehicleId = string.Empty;
        UpdateVendorSectionsAll();
    }

    private bool IsOnLift(Entity<VehicleSupplyLiftComponent> lift, EntityUid entity)
    {
        if (!TryComp(lift.Owner, out TransformComponent? liftXform) ||
            !TryComp(entity, out TransformComponent? entityXform))
        {
            return false;
        }

        var liftCoords = _transform.GetMapCoordinates(lift.Owner, liftXform);
        var entityCoords = _transform.GetMapCoordinates(entity, entityXform);
        if (liftCoords.MapId != entityCoords.MapId)
            return false;

        var radius = lift.Comp.Radius;
        return (entityCoords.Position - liftCoords.Position).LengthSquared() <= radius * radius;
    }

    private void SendConsoleStateAll()
    {
        var query = EntityQueryEnumerator<VehicleSupplyConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            SendConsoleState(uid, comp);
        }
    }

    private void SendConsoleState(EntityUid uid, VehicleSupplyConsoleComponent? console = null)
    {
        if (!Resolve(uid, ref console, logMissing: false))
            return;

        var unlocked = BuildUnlockedSet();
        var available = new List<VehicleSupplyEntryState>();

        VehicleSupplyLiftMode? mode = null;
        var busy = false;
        string? activeId = null;
        string? selectedId = string.IsNullOrWhiteSpace(console.SelectedVehicle) ? null : console.SelectedVehicle;
        var selectedCopyIndex = console.SelectedVehicleCopyIndex;
        VehicleSupplyPreviewState? preview = null;

        var hasLift = TryGetLift(uid, console, out var lift);
        if (hasLift)
        {
            mode = lift.Comp.Mode;
            busy = lift.Comp.Busy;
            activeId = string.IsNullOrWhiteSpace(lift.Comp.ActiveVehicleId) ? null : lift.Comp.ActiveVehicleId;

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                var key = Normalize(selectedId);
                var layers = new List<VehicleHardpointLayerState>();
                var overlays = new List<VehicleSupplyPreviewOverlay>();
                if (TryGetStoredEntity(lift.Comp, key, selectedCopyIndex, out var stored))
                {
                    layers = BuildPreviewLayers(stored);
                    overlays = BuildPreviewOverlays(stored);
                }

                preview = new VehicleSupplyPreviewState(selectedId, selectedCopyIndex, layers, overlays);
            }
        }

        foreach (var entry in console.Vehicles)
        {
            if (!IsEntryUnlocked(entry, unlocked))
                continue;

            if (hasLift)
            {
                var key = Normalize(entry.Vehicle.Id);
                var count = GetStoredCount(lift.Comp, key);
                if (count <= 0)
                    continue;

                available.Add(new VehicleSupplyEntryState(entry.Vehicle.Id, GetEntryName(entry), count));
                continue;
            }

            available.Add(new VehicleSupplyEntryState(entry.Vehicle.Id, GetEntryName(entry), 1));
        }

        var state = new VehicleSupplyBuiState(mode, busy, activeId, selectedId, selectedCopyIndex, preview, available);
        _ui.SetUiState(uid, VehicleSupplyUIKey.Key, state);
    }

    private void UpdateVendorSectionsAll()
    {
        var query = EntityQueryEnumerator<VehicleHardpointVendorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateVendorSections(uid, comp);
        }
    }

    private void UpdateVendorSections(
        EntityUid uid,
        VehicleHardpointVendorComponent? vendor = null,
        CMAutomatedVendorComponent? automated = null)
    {
        if (!Resolve(uid, ref vendor, ref automated, logMissing: false))
            return;

        var hasLift = TryGetLiftForVendor(uid, vendor, out var lift);

        var catalog = BuildVendorCatalog(uid, vendor);
        var unlocked = BuildUnlockedSet();

        var existingAmounts = new Dictionary<EntProtoId, int>();
        foreach (var section in automated.Sections)
        {
            foreach (var entry in section.Entries)
            {
                if (entry.Amount != null)
                    existingAmounts[entry.Id] = entry.Amount.Value;
            }
        }

        var previousCounts = new Dictionary<string, int>(vendor.LastVehicleCounts);
        vendor.LastVehicleCounts.Clear();
        var validGroupStateKeys = new HashSet<string>();

        var sections = new List<CMVendorSection>();
        foreach (var entry in catalog)
        {
            if (!IsEntryUnlocked(entry, unlocked))
                continue;

            var vehicleKey = Normalize(entry.Vehicle.Id);
            var count = hasLift ? GetVendorAvailableVehicleCount(lift.Comp, vehicleKey) : 0;
            var lastCount = previousCounts.TryGetValue(vehicleKey, out var prev) ? prev : 0;
            var delta = count - lastCount;

            var hardpoints = GetHardpointsForVehicle(entry.Vehicle.Id, catalog);
            if (hardpoints.Count == 0)
                continue;

            var hardpointEntries = new List<VendorHardpointEntry>();
            var vehicleName = GetEntryName(entry);
            foreach (var hardpoint in hardpoints)
            {
                if (string.IsNullOrWhiteSpace(hardpoint))
                    continue;

                var displayName = GetPrototypeName(hardpoint);
                var sharedKey = Normalize(hardpoint);
                var order = int.MaxValue;
                var sectionName = vehicleName;
                var sectionOrder = int.MaxValue;

                if (TryGetTankSharedCategory(entry.Vehicle.Id, hardpoint, out var categoryKey, out var categoryLabel, out var categoryOrder))
                {
                    sharedKey = categoryKey;
                    order = categoryOrder;
                    sectionName = $"{vehicleName} - {categoryLabel}";
                    sectionOrder = categoryOrder;
                }

                hardpointEntries.Add(new VendorHardpointEntry(
                    hardpoint,
                    sharedKey,
                    order,
                    displayName,
                    sectionName,
                    sectionOrder));
            }

            if (hardpointEntries.Count == 0)
                continue;

            var groupedBySharedKey = new Dictionary<string, List<EntProtoId>>();
            foreach (var hardpoint in hardpointEntries)
            {
                var id = new EntProtoId(hardpoint.Id);
                if (!groupedBySharedKey.TryGetValue(hardpoint.SharedKey, out var list))
                {
                    list = new List<EntProtoId>();
                    groupedBySharedKey[hardpoint.SharedKey] = list;
                }

                list.Add(id);
            }

            if (count <= 0)
            {
                foreach (var sharedKey in groupedBySharedKey.Keys)
                {
                    var groupStateKey = $"{vehicleKey}:{sharedKey}";
                    vendor.RemainingGroupAmounts.Remove(groupStateKey);
                }

                continue;
            }

            vendor.LastVehicleCounts[vehicleKey] = count;

            var sharedAmounts = new Dictionary<string, int>();
            foreach (var (sharedKey, ids) in groupedBySharedKey)
            {
                var groupStateKey = $"{vehicleKey}:{sharedKey}";
                validGroupStateKeys.Add(groupStateKey);

                var remaining = vendor.RemainingGroupAmounts.TryGetValue(groupStateKey, out var tracked)
                    ? tracked
                    : lastCount;

                var hasExistingForGroup = false;
                var minExisting = int.MaxValue;
                foreach (var id in ids)
                {
                    if (!existingAmounts.TryGetValue(id, out var existing))
                        continue;

                    hasExistingForGroup = true;
                    if (existing < minExisting)
                        minExisting = existing;
                }

                if (hasExistingForGroup)
                    remaining = minExisting;

                if (delta > 0)
                    remaining += delta;

                remaining = Math.Clamp(remaining, 0, count);
                vendor.RemainingGroupAmounts[groupStateKey] = remaining;
                sharedAmounts[sharedKey] = remaining;
            }

            foreach (var sectionGroup in hardpointEntries
                         .GroupBy(h => h.SectionName)
                         .OrderBy(g => g.Min(h => h.SectionOrder))
                         .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var section = new CMVendorSection
                {
                    Name = sectionGroup.Key,
                    Entries = new List<CMVendorEntry>()
                };

                foreach (var hardpoint in sectionGroup
                             .OrderBy(h => h.SortOrder)
                             .ThenBy(h => h.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    if (!sharedAmounts.TryGetValue(hardpoint.SharedKey, out var amount))
                        continue;

                    var id = new EntProtoId(hardpoint.Id);
                    section.Entries.Add(new CMVendorEntry
                    {
                        Id = id,
                        Name = hardpoint.DisplayName,
                        Amount = amount,
                        Multiplier = amount,
                        Max = amount
                    });
                }

                if (section.Entries.Count > 0)
                    sections.Add(section);
            }
        }

        var staleGroupKeys = vendor.RemainingGroupAmounts.Keys
            .Where(key => !validGroupStateKeys.Contains(key))
            .ToArray();
        foreach (var key in staleGroupKeys)
        {
            vendor.RemainingGroupAmounts.Remove(key);
        }

        _vendor.SetSections((uid, automated), sections);
    }

    private bool TryGetTankSharedCategory(
        string vehicleId,
        string hardpointId,
        out string categoryKey,
        out string categoryLabel,
        out int categoryOrder)
    {
        categoryKey = string.Empty;
        categoryLabel = string.Empty;
        categoryOrder = int.MaxValue;

        if (!string.Equals(Normalize(vehicleId), "rmcvehicletank", StringComparison.Ordinal))
            return false;

        var hardpointKey = Normalize(hardpointId);
        if (hardpointKey == "rmcvehicletanksnowplow")
        {
            categoryKey = "tank-general";
            categoryLabel = "General";
            categoryOrder = 3;
            return true;
        }

        if (!_hardpointTypeByProto.TryGetValue(Normalize(hardpointId), out var hardpointType))
            return false;

        switch (Normalize(hardpointType))
        {
            case "cannon":
                categoryKey = "tank-primary";
                categoryLabel = "Primary";
                categoryOrder = 0;
                return true;
            case "launcher":
                categoryKey = "tank-secondary";
                categoryLabel = "Secondary";
                categoryOrder = 1;
                return true;
            case "armor":
                categoryKey = "tank-armor";
                categoryLabel = "Armor";
                categoryOrder = 2;
                return true;
            case "support":
                categoryKey = "tank-support";
                categoryLabel = "Support";
                categoryOrder = 4;
                return true;
            default:
                return false;
        }
    }

    private bool TryGetLiftForVendor(
        EntityUid vendorUid,
        VehicleHardpointVendorComponent vendor,
        out Entity<VehicleSupplyLiftComponent> lift)
    {
        lift = default;
        var found = false;

        var vendorCoords = _transform.GetMapCoordinates(vendorUid);
        var maxDistance = vendor.ConsoleSearchRange * vendor.ConsoleSearchRange;

        if (TryFindLiftForVendor(vendorCoords, maxDistance, true, out var rangedLift))
        {
            lift = rangedLift;
            return true;
        }

        if (TryFindLiftForVendor(vendorCoords, maxDistance, false, out var anyLift))
        {
            lift = anyLift;
            return true;
        }

        return found;
    }

    private bool TryFindLiftForVendor(
        MapCoordinates vendorCoords,
        float maxDistance,
        bool useRange,
        out Entity<VehicleSupplyLiftComponent> lift)
    {
        lift = default;
        var found = false;
        var bestDistance = float.MaxValue;

        var query = EntityQueryEnumerator<VehicleSupplyLiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            var liftCoords = _transform.GetMapCoordinates(uid, xform);
            if (liftCoords.MapId != vendorCoords.MapId)
                continue;

            var distance = (liftCoords.Position - vendorCoords.Position).LengthSquared();
            if (useRange && distance > maxDistance)
                continue;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            lift = (uid, comp);
            found = true;
        }

        return found;
    }

    public bool TryGetAnyLift(out Entity<VehicleSupplyLiftComponent> lift)
    {
        var query = EntityQueryEnumerator<VehicleSupplyLiftComponent>();
        if (query.MoveNext(out var uid, out var comp))
        {
            lift = (uid, comp);
            return true;
        }

        lift = default;
        return false;
    }

    public bool DebugAddVehicleToStorage(EntityUid liftUid, string vehicleId, bool forceUnlock, out string? reason)
    {
        reason = null;

        if (!TryComp(liftUid, out VehicleSupplyLiftComponent? lift))
        {
            reason = $"Entity {liftUid} does not have VehicleSupplyLiftComponent.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(vehicleId))
        {
            reason = "Vehicle id is empty.";
            return false;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(vehicleId, out _))
        {
            reason = $"Unknown vehicle prototype '{vehicleId}'.";
            return false;
        }

        var key = Normalize(vehicleId);

        if (forceUnlock)
        {
            var tech = EnsureSupplyTech();
            if (!tech.Comp.Unlocked.Contains(key))
            {
                tech.Comp.Unlocked.Add(key);
                Dirty(tech);
            }
        }

        AddStored(lift, key);

        Dirty(liftUid, lift);
        SendConsoleStateAll();
        UpdateVendorSectionsAll();
        return true;
    }

    public void DebugEnsureVehicleInConsoles(EntityUid liftUid, string vehicleId)
    {
        if (!_prototypes.TryIndex<EntityPrototype>(vehicleId, out var proto))
            return;

        var mapId = _transform.GetMapId(liftUid);
        var query = EntityQueryEnumerator<VehicleSupplyConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var console, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            if (TryGetEntry(console, vehicleId, out _))
                continue;

            console.Vehicles.Add(new VehicleSupplyEntry
            {
                Vehicle = vehicleId,
                Unlock = vehicleId,
                Name = proto.Name
            });

            SendConsoleState(uid, console);
        }

        UpdateVendorSectionsAll();
    }

    private bool TryGetLift(EntityUid consoleUid, VehicleSupplyConsoleComponent console, out Entity<VehicleSupplyLiftComponent> lift)
    {
        lift = default;
        var found = false;

        var consoleCoords = _transform.GetMapCoordinates(consoleUid);
        var bestDistance = float.MaxValue;

        var query = EntityQueryEnumerator<VehicleSupplyLiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            var liftCoords = _transform.GetMapCoordinates(uid, xform);
            if (liftCoords.MapId != consoleCoords.MapId)
                continue;

            var distance = (liftCoords.Position - consoleCoords.Position).LengthSquared();
            if (distance > console.LiftSearchRange * console.LiftSearchRange)
                continue;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            lift = (uid, comp);
            found = true;
        }

        return found;
    }


    private List<VehicleSupplyEntry> BuildVendorCatalog(EntityUid vendorUid, VehicleHardpointVendorComponent vendor)
    {
        var vendorCoords = _transform.GetMapCoordinates(vendorUid);
        var maxDistance = vendor.ConsoleSearchRange * vendor.ConsoleSearchRange;
        var list = new List<VehicleSupplyEntry>();
        var seen = new HashSet<string>();

        void Collect(bool useRange)
        {
            var query = EntityQueryEnumerator<VehicleSupplyConsoleComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var console, out var xform))
            {
                var consoleCoords = _transform.GetMapCoordinates(uid, xform);
                if (consoleCoords.MapId != vendorCoords.MapId)
                    continue;

                if (useRange)
                {
                    var distance = (consoleCoords.Position - vendorCoords.Position).LengthSquared();
                    if (distance > maxDistance)
                        continue;
                }

                foreach (var entry in console.Vehicles)
                {
                    var key = Normalize(entry.Vehicle.Id);
                    if (seen.Add(key))
                        list.Add(entry);
                }
            }
        }

        Collect(true);
        if (list.Count == 0)
            Collect(false);

        return list;
    }

    private bool TryGetEntry(VehicleSupplyConsoleComponent console, string vehicleId, out VehicleSupplyEntry entry)
    {
        var key = Normalize(vehicleId);
        foreach (var candidate in console.Vehicles)
        {
            if (Normalize(candidate.Vehicle.Id) == key)
            {
                entry = candidate;
                return true;
            }
        }

        entry = default!;
        return false;
    }

    private string GetEntryName(VehicleSupplyEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Name))
            return entry.Name;

        return GetPrototypeName(entry.Vehicle.Id);
    }

    private string GetPrototypeName(string protoId)
    {
        if (_prototypes.TryIndex<EntityPrototype>(protoId, out var proto))
            return proto.Name;

        return protoId;
    }

    private Entity<VehicleSupplyTechComponent> EnsureSupplyTech()
    {
        var query = EntityQueryEnumerator<VehicleSupplyTechComponent>();
        if (query.MoveNext(out var uid, out var comp))
            return (uid, comp);

        var tree = _intel.EnsureTechTree();
        var tech = EnsureComp<VehicleSupplyTechComponent>(tree.Owner);
        return (tree.Owner, tech);
    }

    private List<VehicleHardpointLayerState> BuildPreviewLayers(
        EntityUid vehicle,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return new List<VehicleHardpointLayerState>();

        var layers = new List<VehicleHardpointLayerState>(hardpoints.Slots.Count);
        var indexByLayer = new Dictionary<string, int>();

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            var layer = slot.VisualLayer;
            if (string.IsNullOrWhiteSpace(layer))
                continue;

            var state = string.Empty;
            var usesOverlay = false;
            if (_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) && itemSlot.HasItem)
            {
                var item = itemSlot.Item!.Value;
                state = ResolveVisualState(item, out usesOverlay);
            }

            var key = layer.ToLowerInvariant();
            if (indexByLayer.TryGetValue(key, out var existingIndex))
            {
                if (!string.IsNullOrWhiteSpace(state))
                    layers[existingIndex] = new VehicleHardpointLayerState(layer, state);
                continue;
            }

            indexByLayer[key] = layers.Count;
            if (usesOverlay)
                state = string.Empty;
            layers.Add(new VehicleHardpointLayerState(layer, state));
        }

        return layers;
    }

    private List<VehicleSupplyPreviewOverlay> BuildPreviewOverlays(
        EntityUid vehicle,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
            return new List<VehicleSupplyPreviewOverlay>();

        var overlays = new List<VehicleSupplyPreviewOverlay>();
        var turretOffsets = new Dictionary<string, PreviewOffset>();

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            var item = itemSlot.Item!.Value;
            if (TryGetTurretOverlay(item, 0, out var overlay, out var offset))
            {
                overlays.Add(overlay);
                turretOffsets[slot.Id] = offset;
            }

            if (!TryComp(item, out HardpointSlotsComponent? attachedSlots) ||
                !TryComp(item, out ItemSlotsComponent? attachedItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in attachedSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(turretSlot.Id))
                    continue;

                if (!_itemSlots.TryGetSlot(item, turretSlot.Id, out var turretItemSlot, attachedItemSlots) ||
                    !turretItemSlot.HasItem)
                {
                    continue;
                }

                var child = turretItemSlot.Item!.Value;
                if (!TryGetTurretOverlay(child, 1, out var childOverlay, out var childOffset))
                    continue;

                if (turretOffsets.TryGetValue(slot.Id, out var parentOffset))
                {
                    var combined = CombineOffsets(parentOffset, childOffset);
                    childOverlay = new VehicleSupplyPreviewOverlay(
                        childOverlay.Rsi,
                        childOverlay.State,
                        childOverlay.Order,
                        combined.Base,
                        combined.UseDirectional,
                        combined.North,
                        combined.East,
                        combined.South,
                        combined.West);
                }

                overlays.Add(childOverlay);
            }
        }

        return overlays;
    }

    private bool TryGetTurretOverlay(
        EntityUid item,
        int order,
        out VehicleSupplyPreviewOverlay overlay,
        out PreviewOffset offset)
    {
        overlay = default!;
        offset = default;

        if (!TryComp(item, out VehicleTurretComponent? turret))
            return false;

        if (!turret.ShowOverlay || string.IsNullOrWhiteSpace(turret.OverlayState) || string.IsNullOrWhiteSpace(turret.OverlayRsi))
            return false;

        offset = new PreviewOffset(
            turret.PixelOffset,
            turret.UseDirectionalOffsets,
            turret.PixelOffsetNorth,
            turret.PixelOffsetEast,
            turret.PixelOffsetSouth,
            turret.PixelOffsetWest);

        overlay = new VehicleSupplyPreviewOverlay(
            turret.OverlayRsi,
            turret.OverlayState,
            order,
            offset.Base,
            offset.UseDirectional,
            offset.North,
            offset.East,
            offset.South,
            offset.West);
        return true;
    }

    private static PreviewOffset CombineOffsets(PreviewOffset a, PreviewOffset b)
    {
        var useDirectional = a.UseDirectional || b.UseDirectional;
        var north = (a.UseDirectional ? a.North : Vector2.Zero) + (b.UseDirectional ? b.North : Vector2.Zero);
        var east = (a.UseDirectional ? a.East : Vector2.Zero) + (b.UseDirectional ? b.East : Vector2.Zero);
        var south = (a.UseDirectional ? a.South : Vector2.Zero) + (b.UseDirectional ? b.South : Vector2.Zero);
        var west = (a.UseDirectional ? a.West : Vector2.Zero) + (b.UseDirectional ? b.West : Vector2.Zero);
        return new PreviewOffset(a.Base + b.Base, useDirectional, north, east, south, west);
    }

    private string ResolveVisualState(EntityUid item, out bool usesOverlay, int depth = 0)
    {
        usesOverlay = false;
        if (depth > 2)
            return string.Empty;

        if (TryComp(item, out VehicleTurretComponent? turretOverlay) && turretOverlay.ShowOverlay)
            usesOverlay = true;

        if (TryComp(item, out HardpointSlotsComponent? attachedSlots) &&
            TryComp(item, out ItemSlotsComponent? attachedItemSlots))
        {
            foreach (var slot in attachedSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(slot.Id))
                    continue;

                if (!_itemSlots.TryGetSlot(item, slot.Id, out var itemSlot, attachedItemSlots) || !itemSlot.HasItem)
                    continue;

                var child = itemSlot.Item!.Value;
                var childState = ResolveVisualState(child, out var childOverlay, depth + 1);
                usesOverlay |= childOverlay;
                if (!string.IsNullOrWhiteSpace(childState))
                    return childState;
            }
        }

        if (TryComp(item, out HardpointVisualComponent? visual) &&
            !string.IsNullOrWhiteSpace(visual.VehicleState))
        {
            return visual.VehicleState;
        }

        if (TryComp(item, out VehicleTurretComponent? turret) &&
            !string.IsNullOrWhiteSpace(turret.OverlayState))
        {
            return turret.OverlayState;
        }

        return string.Empty;
    }

    private HashSet<string> BuildUnlockedSet()
    {
        var unlocked = new HashSet<string>();
        var tech = EnsureSupplyTech();
        foreach (var id in tech.Comp.Unlocked)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            unlocked.Add(Normalize(id));
        }

        return unlocked;
    }

    private static bool IsEntryUnlocked(VehicleSupplyEntry entry, HashSet<string> unlocked)
    {
        if (string.IsNullOrWhiteSpace(entry.Unlock))
            return true;

        return unlocked.Contains(Normalize(entry.Unlock));
    }

    private IReadOnlyList<string> GetHardpointsForVehicle(string vehicleId, IReadOnlyList<VehicleSupplyEntry> entries)
    {
        var key = Normalize(vehicleId);
        if (_hardpointsByVehicleCache.TryGetValue(key, out var cached))
            return cached;

        var explicitList = GetExplicitHardpoints(vehicleId, entries);
        if (explicitList != null)
        {
            _hardpointsByVehicleCache[key] = explicitList;
            return explicitList;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(vehicleId, out var vehicleProto))
        {
            _hardpointsByVehicleCache[key] = new List<string>();
            return _hardpointsByVehicleCache[key];
        }

        if (!vehicleProto.TryGetComponent(out HardpointSlotsComponent? slots, _compFactory))
        {
            _hardpointsByVehicleCache[key] = new List<string>();
            return _hardpointsByVehicleCache[key];
        }

        var result = new List<string>();
        var seen = new HashSet<string>();

        foreach (var slot in slots.Slots)
        {
            var typeKey = Normalize(slot.HardpointType);
            if (!_hardpointItemsByType.TryGetValue(typeKey, out var candidates))
                continue;

            var whitelistTags = slot.Whitelist?.Tags;

            foreach (var candidate in candidates)
            {
                if (whitelistTags != null && whitelistTags.Count > 0)
                {
                    var allowed = false;
                    foreach (var tag in whitelistTags)
                    {
                        if (candidate.Tags.Contains(tag))
                        {
                            allowed = true;
                            break;
                        }
                    }

                    if (!allowed)
                        continue;
                }

                if (seen.Add(candidate.ProtoId))
                    result.Add(candidate.ProtoId);
            }
        }

        _hardpointsByVehicleCache[key] = result;
        return result;
    }

    private static List<string>? GetExplicitHardpoints(string vehicleId, IReadOnlyList<VehicleSupplyEntry> entries)
    {
        var key = Normalize(vehicleId);
        foreach (var entry in entries)
        {
            if (Normalize(entry.Vehicle.Id) != key)
                continue;

            if (entry.Hardpoints.Count == 0)
                return null;

            var list = new List<string>(entry.Hardpoints.Count);
            foreach (var hardpoint in entry.Hardpoints)
            {
                if (!string.IsNullOrWhiteSpace(hardpoint.Id))
                    list.Add(hardpoint.Id);
            }

            return list;
        }

        return null;
    }
}
