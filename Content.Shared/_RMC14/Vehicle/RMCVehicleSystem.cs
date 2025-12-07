using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
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
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;

    private readonly Dictionary<EntityUid, InteriorData> _vehicleInteriors = new();
    private readonly Dictionary<MapId, EntityUid> _mapToVehicle = new();

    private sealed class InteriorData
    {
        public EntityUid Map = EntityUid.Invalid;
        public MapId MapId = MapId.Nullspace;
        public EntityCoordinates Entry;
        public EntityUid? EntryEntity;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleEnterComponent, ActivateInWorldEvent>(OnVehicleEnterActivate);
        SubscribeLocalEvent<VehicleEnterComponent, ComponentShutdown>(OnVehicleEnterShutdown);

        SubscribeLocalEvent<VehicleExitComponent, ActivateInWorldEvent>(OnVehicleExitActivate);

        SubscribeLocalEvent<VehicleDriverSeatComponent, StrapAttemptEvent>(OnDriverSeatStrapAttempt);
        SubscribeLocalEvent<VehicleDriverSeatComponent, StrappedEvent>(OnDriverSeatStrapped);
        SubscribeLocalEvent<VehicleDriverSeatComponent, UnstrappedEvent>(OnDriverSeatUnstrapped);
    }

    private void OnVehicleEnterActivate(Entity<VehicleEnterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsClient)
            return;

        args.Handled = TryEnter(ent, args.User);
    }

    private bool TryEnter(Entity<VehicleEnterComponent> ent, EntityUid user)
    {
        if (!_vehicleInteriors.TryGetValue(ent.Owner, out var interior))
        {
            if (!EnsureInterior(ent, out interior))
                return false;
        }

        _transform.SetCoordinates(user, interior.Entry);
        return true;
    }

    private bool EnsureInterior(Entity<VehicleEnterComponent> ent, [NotNullWhen(true)] out InteriorData? interior)
    {
        if (_vehicleInteriors.TryGetValue(ent.Owner, out interior))
            return true;

        interior = null;
        if (_net.IsClient)
            return false;

        if (!_mapLoader.TryLoadMap(ent.Comp.InteriorPath, out var loadedMap, out _))
        {
            Log.Error($"Failed to load interior for {ToPrettyString(ent.Owner)} at {ent.Comp.InteriorPath}");
            return false;
        }

        if (loadedMap is not { } map)
            return false;

        var mapId = map.Comp.MapId;

        EntityUid? entryEnt = null;
        EntityCoordinates entryCoords = new(map.Owner, Vector2.Zero);

        var exitQuery = EntityQueryEnumerator<VehicleExitComponent, TransformComponent>();
        while (exitQuery.MoveNext(out var exitUid, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            entryEnt = exitUid;
            entryCoords = xform.Coordinates;
            break;
        }

        interior = new InteriorData
        {
            Map = map.Owner,
            MapId = mapId,
            Entry = entryCoords,
            EntryEntity = entryEnt,
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

        if (!TryComp(ent, out TransformComponent? exitXform) || exitXform.MapID == MapId.Nullspace)
            return;

        if (!_mapToVehicle.TryGetValue(exitXform.MapID, out var vehicle) || Deleted(vehicle))
        {
            _mapToVehicle.Remove(exitXform.MapID);
            return;
        }

        if (!TryComp(vehicle, out VehicleEnterComponent? enter))
            return;

        var vehicleXform = Transform(vehicle);

        EntityUid? parent = vehicleXform.ParentUid;
        if (parent == null || !parent.Value.IsValid())
            parent = vehicleXform.MapUid;
        if (parent == null || !parent.Value.IsValid())
            return;

        var offset = enter.ExitOffset;
        var rotated = vehicleXform.LocalRotation.RotateVec(offset);
        var position = vehicleXform.LocalPosition + rotated;

        var exitCoords = new EntityCoordinates(parent.Value, position);
        _transform.SetCoordinates(args.User, exitCoords);
        args.Handled = true;
    }

    private void OnDriverSeatStrapAttempt(Entity<VehicleDriverSeatComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_skills.HasSkills(args.Buckle.Owner, ent.Comp.Skills))
            return;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString("rmc-skills-cant-operate", ("target", ent)), args.Buckle, args.User);

        args.Cancelled = true;
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
    }

    private bool TryGetVehicleFromInterior(EntityUid interiorEntity, out EntityUid? vehicle)
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
