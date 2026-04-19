using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Teleporter;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
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

public sealed class VehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly VehicleViewToggleSystem _viewToggle = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCTeleporterSystem _rmcTeleporter = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly Content.Shared.Vehicle.VehicleSystem _vehicles = default!;
    [Dependency] private readonly VehicleLockSystem _vehicleLock = default!;

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
        SubscribeLocalEvent<VehicleInteriorOccupantComponent, ComponentStartup>(OnOccupantStartup);
        SubscribeLocalEvent<VehicleInteriorOccupantComponent, ComponentRemove>(OnOccupantRemove);
        SubscribeLocalEvent<VehicleInteriorOccupantComponent, MapUidChangedEvent>(OnOccupantMapChanged);
        SubscribeLocalEvent<VehicleInteriorOccupantComponent, MetaFlagRemoveAttemptEvent>(OnOccupantMetaFlagRemoveAttempt);
        SubscribeLocalEvent<HardpointIntegrityComponent, VehicleCanRunEvent>(OnFrameVehicleCanRun);
        SubscribeLocalEvent<RMCConstructionAttemptEvent>(OnConstructionAttempt);
    }

    private void OnVehicleEnterActivate(Entity<VehicleEnterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_net.IsClient)
            return;

        if (IsEntryBlockedByLock(ent.Owner, args.User))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-locked"), args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (args.Handled)
        {
            return;
        }

        if (!TryFindEntry(ent, args.User, out var entryIndex))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-use-doorway"), args.User, args.User);
            return;
        }

        var interior = EnsureComp<VehicleInteriorComponent>(ent.Owner);

        if (!interior.EntryLocks.Add(entryIndex))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-busy"), args.User, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.EnterDoAfter, new VehicleEnterDoAfterEvent { EntryIndex = entryIndex }, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            interior.EntryLocks.Remove(entryIndex);
            return;
        }

        args.Handled = true;
    }

    private bool TryEnter(Entity<VehicleEnterComponent> ent, EntityUid user, int entryIndex = -1)
    {
        if (IsEntryBlockedByLock(ent.Owner, user))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-locked"), user, user, PopupType.SmallCaution);
            return false;
        }

        if (!EnsureInterior(ent, out var interior))
            return false;

        PruneTrackedOccupants(ent.Owner, interior);

        var isXeno = HasComp<XenoComponent>(user);
        if (isXeno)
        {
            if (ent.Comp.MaxXenos > 0 &&
                !interior.Xenos.Contains(user) &&
                CountLivingOccupants(interior.Xenos) >= ent.Comp.MaxXenos)
            {
                _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-xeno-full"), user, user);
                return false;
            }
        }
        else
        {
            if (ent.Comp.MaxPassengers > 0 &&
                !interior.Passengers.Contains(user) &&
                CountLivingOccupants(interior.Passengers) >= ent.Comp.MaxPassengers)
            {
                _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-passenger-full"), user, user);
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
                TrackOccupant(user, ent.Owner, isXeno);
                return true;
            }
        }

        targetMapCoords = _transform.ToMapCoordinates(coords);
        _rmcTeleporter.HandlePulling(user, targetMapCoords);
        TrackOccupant(user, ent.Owner, isXeno);
        return true;
    }

    private bool EnsureInterior(Entity<VehicleEnterComponent> ent, [NotNullWhen(true)] out VehicleInteriorComponent? interior)
    {
        if (TryComp(ent.Owner, out interior) &&
            interior.MapId != MapId.Nullspace &&
            _mapManager.MapExists(interior.MapId))
        {
            return true;
        }

        interior = null;
        if (_net.IsClient)
            return false;

        interior = EnsureComp<VehicleInteriorComponent>(ent.Owner);

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
        var mapUid = map.Owner;

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

        interior.Map = mapUid;
        interior.MapId = mapId;
        interior.Entry = entryCoords;
        interior.EntryParent = entryParent;
        interior.Grid = interiorGrid;
        interior.Passengers.Clear();
        interior.Xenos.Clear();

        var link = EnsureComp<VehicleInteriorLinkComponent>(mapUid);
        link.Vehicle = ent.Owner;

        return true;
    }

    private void OnVehicleEnterShutdown(Entity<VehicleEnterComponent> ent, ref ComponentShutdown args)
    {
        CleanupInterior(ent.Owner);
    }

    private void CleanupInterior(EntityUid vehicle)
    {
        if (!TryComp(vehicle, out VehicleInteriorComponent? interior))
            return;

        foreach (var passenger in new List<EntityUid>(interior.Passengers))
        {
            if (TryComp(passenger, out VehicleInteriorOccupantComponent? occupant) &&
                occupant.Vehicle == vehicle)
            {
                RemComp<VehicleInteriorOccupantComponent>(passenger);
            }
        }

        foreach (var xeno in new List<EntityUid>(interior.Xenos))
        {
            if (TryComp(xeno, out VehicleInteriorOccupantComponent? occupant) &&
                occupant.Vehicle == vehicle)
            {
                RemComp<VehicleInteriorOccupantComponent>(xeno);
            }
        }

        if (interior.Map.IsValid() &&
            EntityManager.EntityExists(interior.Map) &&
            TryComp(interior.Map, out VehicleInteriorLinkComponent? link) &&
            link.Vehicle == vehicle)
        {
            RemComp<VehicleInteriorLinkComponent>(interior.Map);
        }

        RemComp<VehicleInteriorComponent>(vehicle);

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
        if (_net.IsClient)
            return;

        if (args.Handled)
            return;

        if (ent.Comp.PendingExit)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-exit-busy"), args.User, args.User);
            return;
        }

        if (!TryComp(ent, out TransformComponent? exitXform) || exitXform.MapID == MapId.Nullspace)
            return;

        if (!TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is not { } vehicleUid)
            return;

        if (!TryComp(vehicleUid, out VehicleEnterComponent? enter))
            return;

        if (IsExitBlockedByLock(vehicleUid, args.User))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-locked"), args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        ent.Comp.PendingExit = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, enter.ExitDoAfter, new VehicleExitDoAfterEvent(), ent.Owner)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ent.Comp.PendingExit = false;
            return;
        }

        args.Handled = true;
    }

    private bool TryFindEntry(Entity<VehicleEnterComponent> ent, EntityUid user, out int entryIndex)
    {
        entryIndex = -1;

        if (ent.Comp.EntryPoints.Count == 0)
            return true;

        var bypassEntry = TryComp(ent.Owner, out HardpointIntegrityComponent? frameIntegrity) &&
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
        if (TryComp(ent.Owner, out VehicleInteriorComponent? interior))
            interior.EntryLocks.Remove(args.EntryIndex);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryEnter(ent, args.User, args.EntryIndex);
    }

    private bool TryExit(Entity<VehicleExitComponent> ent, EntityUid user)
    {
        if (!TryComp(ent, out TransformComponent? exitXform) || exitXform.MapID == MapId.Nullspace)
            return false;

        if (!TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is not { } vehicleUid)
            return false;

        if (!TryComp(vehicleUid, out VehicleEnterComponent? enter))
            return false;

        if (IsExitBlockedByLock(vehicleUid, user))
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-enter-locked"), user, user, PopupType.SmallCaution);
            return false;
        }

        var vehicleXform = Transform(vehicleUid);

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
        UntrackOccupant(user, vehicleUid);
        return true;
    }

    private void OnVehicleExitDoAfter(Entity<VehicleExitComponent> ent, ref VehicleExitDoAfterEvent args)
    {
        ent.Comp.PendingExit = false;

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = TryExit(ent, args.User);
    }

    private void OnOccupantStartup(Entity<VehicleInteriorOccupantComponent> ent, ref ComponentStartup args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }

    private void OnOccupantRemove(Entity<VehicleInteriorOccupantComponent> ent, ref ComponentRemove args)
    {
        _meta.RemoveFlag(ent, MetaDataFlags.ExtraTransformEvents);

        if (ent.Comp.Vehicle.IsValid())
            UnregisterTrackedOccupant(ent.Comp.Vehicle, ent.Owner, ent.Comp.IsXeno);
    }

    private void OnOccupantMapChanged(Entity<VehicleInteriorOccupantComponent> ent, ref MapUidChangedEvent args)
    {
        if (ent.Comp.Vehicle == EntityUid.Invalid)
            return;

        if (TryComp(ent.Comp.Vehicle, out VehicleInteriorComponent? interior) &&
            args.NewMapId == interior.MapId)
        {
            RegisterTrackedOccupant(ent.Comp.Vehicle, ent.Owner, ent.Comp.IsXeno, interior);
            return;
        }

        RemCompDeferred<VehicleInteriorOccupantComponent>(ent.Owner);
    }

    private void OnOccupantMetaFlagRemoveAttempt(Entity<VehicleInteriorOccupantComponent> ent, ref MetaFlagRemoveAttemptEvent args)
    {
        if ((args.ToRemove & MetaDataFlags.ExtraTransformEvents) != 0 &&
            ent.Comp.LifeStage <= ComponentLifeStage.Running)
        {
            args.ToRemove &= ~MetaDataFlags.ExtraTransformEvents;
        }
    }

    private void TrackOccupant(EntityUid user, EntityUid vehicle, bool isXeno)
    {
        var occupant = EnsureComp<VehicleInteriorOccupantComponent>(user);
        if (occupant.Vehicle.IsValid() &&
            occupant.Vehicle != vehicle)
        {
            UnregisterTrackedOccupant(occupant.Vehicle, user, occupant.IsXeno);
        }

        occupant.Vehicle = vehicle;
        occupant.IsXeno = isXeno;
        RegisterTrackedOccupant(vehicle, user, isXeno);
    }

    private void UntrackOccupant(EntityUid user, EntityUid vehicle)
    {
        if (!TryComp(user, out VehicleInteriorOccupantComponent? occupant) ||
            occupant.Vehicle != vehicle)
        {
            UnregisterTrackedOccupant(vehicle, user, HasComp<XenoComponent>(user));
            return;
        }

        RemComp<VehicleInteriorOccupantComponent>(user);
    }

    private void RegisterTrackedOccupant(
        EntityUid vehicle,
        EntityUid user,
        bool isXeno,
        VehicleInteriorComponent? interior = null)
    {
        if (!Resolve(vehicle, ref interior, logMissing: false))
            return;

        if (isXeno)
        {
            interior.Passengers.Remove(user);
            interior.Xenos.Add(user);
        }
        else
        {
            interior.Xenos.Remove(user);
            interior.Passengers.Add(user);
        }
    }

    private void UnregisterTrackedOccupant(EntityUid vehicle, EntityUid user, bool isXeno)
    {
        if (!TryComp(vehicle, out VehicleInteriorComponent? interior))
            return;

        if (isXeno)
            interior.Xenos.Remove(user);
        else
            interior.Passengers.Remove(user);
    }

    private void PruneTrackedOccupants(EntityUid vehicle, VehicleInteriorComponent interior)
    {
        foreach (var passenger in new List<EntityUid>(interior.Passengers))
        {
            if (TryComp(passenger, out VehicleInteriorOccupantComponent? occupant) &&
                occupant.Vehicle == vehicle &&
                !occupant.IsXeno &&
                _transform.GetMapId(passenger) == interior.MapId)
            {
                continue;
            }

            interior.Passengers.Remove(passenger);
        }

        foreach (var xeno in new List<EntityUid>(interior.Xenos))
        {
            if (TryComp(xeno, out VehicleInteriorOccupantComponent? occupant) &&
                occupant.Vehicle == vehicle &&
                occupant.IsXeno &&
                _transform.GetMapId(xeno) == interior.MapId)
            {
                continue;
            }

            interior.Xenos.Remove(xeno);
        }
    }

    private int CountLivingOccupants(HashSet<EntityUid> occupants)
    {
        var count = 0;
        foreach (var occupant in occupants)
        {
            if (!_mobState.IsDead(occupant))
                count++;
        }

        return count;
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
        _vehicleLock.EnableLockAction(args.Buckle.Owner, vehicle.Value);
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

        _vehicleLock.DisableLockAction(args.Buckle.Owner, vehicle.Value);

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
        _viewToggle.EnableViewToggle(ent.Owner, args.Vehicle.Owner, args.Vehicle.Owner, insideTarget: null, isOutside: true);
    }

    private void OnVehicleOperatorExited(Entity<VehicleOperatorComponent> ent, ref OnVehicleExitedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent, out EyeComponent? eye))
            return;

        _viewToggle.DisableViewToggle(ent.Owner, args.Vehicle.Owner);

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

    private void OnFrameVehicleCanRun(Entity<HardpointIntegrityComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun || ent.Comp.Integrity > 0f)
            return;

        args.CanRun = false;
    }

    private void OnConstructionAttempt(ref RMCConstructionAttemptEvent ev)
    {
        if (ev.Cancelled ||
            _net.IsClient ||
            !TryGetVehicleFromInterior(ev.Location.EntityId, out _))
        {
            return;
        }

        ev.Cancelled = true;
        ev.Popup = Loc.GetString("construction-system-inside-container");
    }

    private bool IsEntryBlockedByLock(EntityUid vehicle, EntityUid user)
    {
        if (!TryComp(vehicle, out VehicleLockComponent? vehicleLock) || !vehicleLock.Locked)
            return false;

        return !CanBypassLockWithDestroyedFrame(vehicle, user);
    }

    private bool IsExitBlockedByLock(EntityUid vehicle, EntityUid user)
    {
        if (!TryComp(vehicle, out VehicleLockComponent? vehicleLock) || !vehicleLock.Locked)
            return false;

        return !CanBypassLockWithDestroyedFrame(vehicle, user);
    }

    private bool CanBypassLockWithDestroyedFrame(EntityUid vehicle, EntityUid user)
    {
        if (!HasComp<XenoComponent>(user))
            return false;

        if (!TryComp(vehicle, out HardpointIntegrityComponent? frameIntegrity))
            return false;

        return frameIntegrity.BypassEntryOnZero && frameIntegrity.Integrity <= 0f;
    }

    public bool TryGetVehicleFromInterior(EntityUid interiorEntity, out EntityUid? vehicle)
    {
        vehicle = null;
        var mapId = _transform.GetMapId(interiorEntity);
        if (mapId == MapId.Nullspace || !_mapManager.MapExists(mapId))
            return false;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        if (!TryComp(mapUid, out VehicleInteriorLinkComponent? link) ||
            Deleted(link.Vehicle))
        {
            return false;
        }

        vehicle = link.Vehicle;
        return true;
    }

    public bool TryResolveControlledVehicle(EntityUid user, out EntityUid vehicle)
    {
        vehicle = EntityUid.Invalid;

        if (TryComp<VehicleOperatorComponent>(user, out var op) &&
            op.Vehicle is { } operatedVehicle &&
            EntityManager.EntityExists(operatedVehicle))
        {
            vehicle = operatedVehicle;
            return true;
        }

        if (!TryGetVehicleFromInterior(user, out var interiorVehicle) ||
            interiorVehicle is not { } interiorVehicleUid ||
            !EntityManager.EntityExists(interiorVehicleUid))
        {
            return false;
        }

        vehicle = interiorVehicleUid;
        return true;
    }

    public bool TryGetInteriorMapId(EntityUid vehicle, out MapId mapId)
    {
        mapId = MapId.Nullspace;
        if (!TryComp(vehicle, out VehicleInteriorComponent? interior))
            return false;

        mapId = interior.MapId;
        return mapId != MapId.Nullspace;
    }
}
