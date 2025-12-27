using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Teleporter;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCTeleporterSystem _rmcTeleporter = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;

    private readonly Dictionary<EntityUid, InteriorData> _vehicleInteriors = new();
    private readonly Dictionary<MapId, EntityUid> _mapToVehicle = new();
    private readonly Dictionary<EntityUid, HashSet<int>> _entryLocks = new();
    private readonly HashSet<EntityUid> _exitLocks = new();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _vehiclePassengers = new();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _vehicleXenos = new();

    private sealed class InteriorData
    {
        public EntityUid Map = EntityUid.Invalid;
        public MapId MapId = MapId.Nullspace;
        public EntityCoordinates Entry;
        public EntityUid EntryParent = EntityUid.Invalid;
        public EntityUid Grid = EntityUid.Invalid;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleEnterComponent, ActivateInWorldEvent>(OnVehicleEnterActivate);
        SubscribeLocalEvent<VehicleEnterComponent, ComponentShutdown>(OnVehicleEnterShutdown);
        SubscribeLocalEvent<VehicleExitComponent, ActivateInWorldEvent>(OnVehicleExitActivate);
        SubscribeLocalEvent<VehicleEnterComponent, VehicleEnterDoAfterEvent>(OnVehicleEnterDoAfter);
        SubscribeLocalEvent<VehicleExitComponent, VehicleExitDoAfterEvent>(OnVehicleExitDoAfter);

        SubscribeLocalEvent<VehicleDriverSeatComponent, StrapAttemptEvent>(OnDriverSeatStrapAttempt);
        SubscribeLocalEvent<VehicleDriverSeatComponent, StrappedEvent>(OnDriverSeatStrapped);
        SubscribeLocalEvent<VehicleDriverSeatComponent, UnstrappedEvent>(OnDriverSeatUnstrapped);

        SubscribeLocalEvent<VehicleOperatorComponent, OnVehicleEnteredEvent>(OnVehicleOperatorEntered);
        SubscribeLocalEvent<VehicleOperatorComponent, OnVehicleExitedEvent>(OnVehicleOperatorExited);
    }

    private void OnVehicleEnterActivate(Entity<VehicleEnterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsClient)
            return;

        if (!TryFindEntry(ent, args.User, out var entryIndex))
        {
            _popup.PopupClient("You need to use a doorway to enter.", args.User, args.User);
            return;
        }

        if (!_entryLocks.TryGetValue(ent.Owner, out var locks))
        {
            locks = new HashSet<int>();
            _entryLocks[ent.Owner] = locks;
        }

        if (locks.Contains(entryIndex))
        {
            _popup.PopupClient("Someone is already entering there.", args.User, args.User);
            return;
        }

        locks.Add(entryIndex);

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.EnterDoAfter, new VehicleEnterDoAfterEvent { EntryIndex = entryIndex }, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            locks.Remove(entryIndex);
            return;
        }

        args.Handled = true;
    }

    private bool TryEnter(Entity<VehicleEnterComponent> ent, EntityUid user, int entryIndex = -1)
    {
        if (!_vehicleInteriors.TryGetValue(ent.Owner, out var interior))
        {
            if (!EnsureInterior(ent, out interior))
                return false;
        }

        EnsureOccupantTracking(ent.Owner, interior, out var passengers, out var xenos);

        var isXeno = HasComp<XenoComponent>(user);
        if (isXeno)
        {
            if (ent.Comp.MaxXenos > 0 && !xenos.Contains(user) && xenos.Count >= ent.Comp.MaxXenos)
            {
                _popup.PopupClient("There's no room for more xenos inside.", user, user);
                return false;
            }
        }
        else
        {
            if (ent.Comp.MaxPassengers > 0 && !passengers.Contains(user) && passengers.Count >= ent.Comp.MaxPassengers)
            {
                _popup.PopupClient("There's no room for more passengers inside.", user, user);
                return false;
            }
        }

        var coords = interior.Entry;
        MapCoordinates targetMapCoords;
        if (entryIndex >= 0 && entryIndex < ent.Comp.EntryPoints.Count)
        {
            var entryPoint = ent.Comp.EntryPoints[entryIndex];
            if (entryPoint.InteriorCoords is { } interiorCoord)
            {
                var parent = interior.Grid.IsValid() ? interior.Grid : interior.EntryParent;
                var entityCoords = new EntityCoordinates(parent, interiorCoord);
                targetMapCoords = _transform.ToMapCoordinates(entityCoords);
                _rmcTeleporter.HandlePulling(user, targetMapCoords);
                if (isXeno)
                    xenos.Add(user);
                else
                    passengers.Add(user);
                return true;
            }
        }

        targetMapCoords = _transform.ToMapCoordinates(coords);
        _rmcTeleporter.HandlePulling(user, targetMapCoords);
        if (isXeno)
            xenos.Add(user);
        else
            passengers.Add(user);
        return true;
    }

    private bool EnsureInterior(Entity<VehicleEnterComponent> ent, [NotNullWhen(true)] out InteriorData? interior)
    {
        if (_vehicleInteriors.TryGetValue(ent.Owner, out interior))
            return true;

        interior = null;
        if (_net.IsClient)
            return false;

        var deserializeOptions = new DeserializationOptions
        {
            InitializeMaps = true,
        };

        if (!_mapLoader.TryLoadMap(ent.Comp.InteriorPath, out var loadedMap, out _, deserializeOptions))
        {
            Log.Error($"[VehicleEnter] Failed to load interior for {ToPrettyString(ent.Owner)} at {ent.Comp.InteriorPath}");
            return false;
        }

        if (loadedMap is not { } map)
            return false;

        var mapId = map.Comp.MapId;

        EntityUid entryParent = map.Owner;
        EntityUid interiorGrid = EntityUid.Invalid;
        var gridEnum = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (gridEnum.MoveNext(out var gridUid, out _, out var gridXform))
        {
            if (gridXform.MapID != mapId)
                continue;

            entryParent = gridUid;
            interiorGrid = gridUid;
            break;
        }

        var entryCoords = new EntityCoordinates(entryParent, Vector2.Zero);

        var exitQuery = EntityQueryEnumerator<VehicleExitComponent, TransformComponent>();
        while (exitQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            entryCoords = xform.Coordinates;
            entryParent = xform.ParentUid.IsValid() ? xform.ParentUid : entryParent;
            break;
        }

        interior = new InteriorData
        {
            Map = map.Owner,
            MapId = mapId,
            Entry = entryCoords,
            EntryParent = entryParent,
            Grid = interiorGrid,
        };

        _vehicleInteriors[ent.Owner] = interior;
        _mapToVehicle[mapId] = ent.Owner;

        return true;
    }

    private void OnVehicleEnterShutdown(Entity<VehicleEnterComponent> ent, ref ComponentShutdown args)
    {
        CleanupInterior(ent.Owner);
    }

    private void CleanupInterior(EntityUid vehicle)
    {
        if (!_vehicleInteriors.Remove(vehicle, out var interior))
            return;

        _entryLocks.Remove(vehicle);
        _vehiclePassengers.Remove(vehicle);
        _vehicleXenos.Remove(vehicle);

        _mapToVehicle.Remove(interior.MapId);

        if (_net.IsClient)
            return;

        if (interior.MapId != MapId.Nullspace && _mapManager.MapExists(interior.MapId))
        {
            _mapManager.DeleteMap(interior.MapId);
        }
        else if (interior.Map.IsValid() && EntityManager.EntityExists(interior.Map))
        {
            Del(interior.Map);
        }
    }

    private void OnVehicleExitActivate(Entity<VehicleExitComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsClient)
            return;

        if (_exitLocks.Contains(ent.Owner))
        {
            _popup.PopupClient("Someone is already using this exit.", args.User, args.User);
            return;
        }

        if (!TryComp(ent, out TransformComponent? exitXform) || exitXform.MapID == MapId.Nullspace)
            return;

        if (!_mapToVehicle.TryGetValue(exitXform.MapID, out var vehicle) || Deleted(vehicle))
        {
            _mapToVehicle.Remove(exitXform.MapID);
            return;
        }

        if (!TryComp(vehicle, out VehicleEnterComponent? enter))
            return;

        _exitLocks.Add(ent.Owner);

        var doAfter = new DoAfterArgs(EntityManager, args.User, enter.ExitDoAfter, new VehicleExitDoAfterEvent(), ent.Owner)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            _exitLocks.Remove(ent.Owner);
            return;
        }

        args.Handled = true;
    }

    private bool TryFindEntry(Entity<VehicleEnterComponent> ent, EntityUid user, out int entryIndex)
    {
        entryIndex = -1;

        if (ent.Comp.EntryPoints.Count == 0)
            return true;

        var bypassEntry = TryComp(ent.Owner, out RMCHardpointIntegrityComponent? frameIntegrity) &&
                          frameIntegrity.BypassEntryOnZero &&
                          frameIntegrity.Integrity <= 0f;

        var vehicleXform = Transform(ent.Owner);
        var userXform = Transform(user);

        if (vehicleXform.MapID != userXform.MapID || vehicleXform.MapID == MapId.Nullspace)
            return false;

        var vehiclePos = _transform.GetWorldPosition(vehicleXform);
        var userPos = _transform.GetWorldPosition(userXform);
        var delta = userPos - vehiclePos;
        var localDelta = (-vehicleXform.LocalRotation).RotateVec(delta);

        if (bypassEntry)
        {
            var closestDistance = float.MaxValue;
            var closestIndex = -1;

            for (var i = 0; i < ent.Comp.EntryPoints.Count; i++)
            {
                var entry = ent.Comp.EntryPoints[i];
                var distance = (localDelta - entry.Offset).LengthSquared();
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            if (closestIndex >= 0)
            {
                entryIndex = closestIndex;
                return true;
            }

            return false;
        }

        for (var i = 0; i < ent.Comp.EntryPoints.Count; i++)
        {
            var entry = ent.Comp.EntryPoints[i];
            if ((localDelta - entry.Offset).Length() <= entry.Radius)
            {
                entryIndex = i;
                return true;
            }
        }

        return false;
    }

    private void OnVehicleEnterDoAfter(Entity<VehicleEnterComponent> ent, ref VehicleEnterDoAfterEvent args)
    {
        if (_entryLocks.TryGetValue(ent.Owner, out var locks))
            locks.Remove(args.EntryIndex);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryEnter(ent, args.User, args.EntryIndex);
    }

    private bool TryExit(Entity<VehicleExitComponent> ent, EntityUid user)
    {
        if (!TryComp(ent, out TransformComponent? exitXform) || exitXform.MapID == MapId.Nullspace)
            return false;

        if (!_mapToVehicle.TryGetValue(exitXform.MapID, out var vehicle) || Deleted(vehicle))
        {
            _mapToVehicle.Remove(exitXform.MapID);
            return false;
        }

        if (!TryComp(vehicle, out VehicleEnterComponent? enter))
            return false;

        var vehicleXform = Transform(vehicle);

        EntityUid? parent = vehicleXform.ParentUid;
        if (parent == null || !parent.Value.IsValid())
            parent = vehicleXform.MapUid;
        if (parent == null || !parent.Value.IsValid())
            return false;

        Vector2 offset;

        var entryIndex = ent.Comp.EntryIndex;
        if (entryIndex >= 0 && entryIndex < enter.EntryPoints.Count)
        {
            offset = enter.EntryPoints[entryIndex].Offset;
        }
        else
        {
            offset = enter.ExitOffset;
        }

        var rotated = vehicleXform.LocalRotation.RotateVec(offset);
        var position = vehicleXform.LocalPosition + rotated;

        var exitCoords = new EntityCoordinates(parent.Value, position);
        var exitMapCoords = _transform.ToMapCoordinates(exitCoords);
        _rmcTeleporter.HandlePulling(user, exitMapCoords);
        if (_vehiclePassengers.TryGetValue(vehicle, out var passengers))
            passengers.Remove(user);
        if (_vehicleXenos.TryGetValue(vehicle, out var xenos))
            xenos.Remove(user);
        return true;
    }

    private void OnVehicleExitDoAfter(Entity<VehicleExitComponent> ent, ref VehicleExitDoAfterEvent args)
    {
        _exitLocks.Remove(ent.Owner);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryExit(ent, args.User);
    }

    private void EnsureOccupantTracking(EntityUid vehicle, InteriorData interior, out HashSet<EntityUid> passengers, out HashSet<EntityUid> xenos)
    {
        if (!_vehiclePassengers.TryGetValue(vehicle, out passengers!) || passengers == null)
        {
            passengers = new HashSet<EntityUid>();
            _vehiclePassengers[vehicle] = passengers;
        }
        else
        {
            passengers.Clear();
        }

        if (!_vehicleXenos.TryGetValue(vehicle, out xenos!) || xenos == null)
        {
            xenos = new HashSet<EntityUid>();
            _vehicleXenos[vehicle] = xenos;
        }
        else
        {
            xenos.Clear();
        }

        if (interior.MapId == MapId.Nullspace)
            return;

        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != interior.MapId)
                continue;
            if (HasComp<XenoComponent>(uid))
                xenos.Add(uid);
            else
                passengers.Add(uid);
        }
    }

    private void OnDriverSeatStrapAttempt(Entity<VehicleDriverSeatComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_skills.HasSkills(args.Buckle.Owner, ent.Comp.Skills))
            return;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString("rmc-skills-cant-operate", ("target", ent)), args.Buckle, args.User);

        //args.Cancelled = true;
    }

    private void OnDriverSeatStrapped(Entity<VehicleDriverSeatComponent> ent, ref StrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetVehicleFromInterior(ent.Owner, out var vehicle) ||
            !TryComp(vehicle, out VehicleComponent? vehicleComp))
        {
            return;
        }

        _vehicles.TrySetOperator((vehicle.Value, vehicleComp), args.Buckle.Owner);

        EnsureComp<VehicleOperatorComponent>(args.Buckle.Owner);
    }

    private void OnDriverSeatUnstrapped(Entity<VehicleDriverSeatComponent> ent, ref UnstrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetVehicleFromInterior(ent.Owner, out var vehicle) ||
            !TryComp(vehicle, out VehicleComponent? vehicleComp))
        {
            return;
        }

        if (vehicleComp.Operator != args.Buckle.Owner)
            return;

        _vehicles.TryRemoveOperator((vehicle.Value, vehicleComp));

        if (!IsOperatingOtherVehicle(args.Buckle.Owner))
        {
            RemCompDeferred<VehicleOperatorComponent>(args.Buckle.Owner);
        }
    }

    private void OnVehicleOperatorEntered(Entity<VehicleOperatorComponent> ent, ref OnVehicleEnteredEvent args)
    {
        if (_net.IsClient)
            return;

        if (!HasComp<VehicleEnterComponent>(args.Vehicle.Owner))
            return;

        _eye.SetTarget(ent.Owner, args.Vehicle.Owner);
    }

    private void OnVehicleOperatorExited(Entity<VehicleOperatorComponent> ent, ref OnVehicleExitedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent, out EyeComponent? eye))
            return;

        if (eye.Target != args.Vehicle.Owner)
            return;

        _eye.SetTarget(ent.Owner, null, eye);
    }

    private bool IsOperatingOtherVehicle(EntityUid entity)
    {
        if (!TryComp<BuckleComponent>(entity, out var buckle))
            return false;

        if (buckle.BuckledTo == null)
            return false;

        return HasComp<VehicleDriverSeatComponent>(buckle.BuckledTo);
    }

    public bool TryGetVehicleFromInterior(EntityUid interiorEntity, out EntityUid? vehicle)
    {
        vehicle = null;
        var mapId = _transform.GetMapId(interiorEntity);
        if (mapId == MapId.Nullspace)
            return false;

        if (!_mapToVehicle.TryGetValue(mapId, out var vehicleUid))
            return false;

        vehicle = vehicleUid;
        return true;
    }
}
