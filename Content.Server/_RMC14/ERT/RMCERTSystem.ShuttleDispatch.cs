using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server._RMC14.Marines;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.ERT;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

public sealed partial class RMCERTSystem
{

    private void Dispatch(Guid id)
    {
        if (!_requests.TryGetValue(id, out var request))
            return;

        if (request.State != RMCERTRequestState.PendingDispatch)
            return;

        if (request.SelectedCall is not { } callId || !_prototypes.TryIndex(callId, out var call))
        {
            FailRequest(request, Loc.GetString("rmc-ert-error-selected-call-missing"));
            return;
        }

        request.State = RMCERTRequestState.Spawning;
        DirtyState(request);

        // Build the shuttle and the final roster up front so ghost-role raffles reflect the team that will actually deploy.
        if (!TryPrepareRequestForDispatch(request, call, false, out var error))
        {
            FailRequest(request, error);
            return;
        }

        var shuttle = request.Shuttle;
        request.PlannedRoster.Clear();
        request.SpawnedGhostRoles.Clear();

        if (IsCargoOnlyShuttle(call))
        {
            if (!SpawnShuttleCargo(request, call, shuttle, out error))
            {
                FailRequest(request, error);
                return;
            }

            request.State = RMCERTRequestState.Recruiting;
            request.RecruitmentEndsAt = _timing.CurTime;
            request.NextAutoLaunchAttempt = _timing.CurTime;
            AddERTRequestLog(LogImpact.High,
                "cargo shuttle loaded",
                request,
                "system",
                call,
                $"cargo={call.ShuttleCargoCount}, shuttle={FormatEntity(shuttle)}");
            if (request.SourceEntity is { Valid: true } cargoBeacon &&
                TryComp(cargoBeacon, out RMCERTDistressBeaconComponent? cargoBeaconComp) &&
                cargoBeaconComp.SingleUse)
            {
                cargoBeaconComp.Spent = true;
            }

            DirtyState(request);
            TryLaunchRequest(request, call, Loc.GetString("rmc-ert-launcher-automatic"));
            return;
        }

        if (!BuildRoster(request, call, out error))
        {
            FailRequest(request, error);
            return;
        }

        if (!SpawnRosterSlots(request, call, shuttle, out error))
        {
            FailRequest(request, error);
            return;
        }

        request.State = RMCERTRequestState.Recruiting;
        request.RecruitmentEndsAt = _timing.CurTime + call.Requirements.RecruitmentDuration;
        request.NextAutoLaunchAttempt = request.RecruitmentEndsAt.Value;
        Log.Info($"ERT request {request.Id} recruiting {request.PlannedRoster.Count} slots for {call.ID}. " +
                 $"RecruitmentDuration={call.Requirements.RecruitmentDuration}, " +
                 $"AutoLaunch={call.AutoLaunch}, MinRequiredSlots={call.Requirements.MinRequiredSlots}");
        AddERTRequestLog(LogImpact.Medium,
            "recruiting started",
            request,
            "system",
            call,
            $"slots={request.PlannedRoster.Count}, recruitmentDuration={call.Requirements.RecruitmentDuration.TotalSeconds:0}s, autoLaunch={call.AutoLaunch}");

        if (request.SourceEntity is { Valid: true } beacon &&
            TryComp(beacon, out RMCERTDistressBeaconComponent? beaconComp) &&
            beaconComp.SingleUse)
        {
            beaconComp.Spent = true;
        }

        DirtyState(request);

        // Auto-launch retries are polled from Update instead of chained through Timer.Spawn.
        // TimerManager processes newly-added timers in the same update pass, so adding a short
        // retry timer from a timer callback can snowball if the server has a long frame.
    }

    private static bool IsCargoOnlyShuttle(RMCERTCallPrototype call)
    {
        return call.Roles.Count == 0 &&
               call.ShuttleCargo.Count > 0;
    }

    private bool TryPrepareRequestForDispatch(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        bool approving,
        out string error)
    {
        error = string.Empty;

        if (!TryEnsureShuttleLoaded(request, call, out var shuttle, out var loadedForPreflight, out error))
            return false;

        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
            return true;

        if (!TryFindNavigationComputer(shuttleUid, out var computer))
        {
            error = Loc.GetString("rmc-ert-error-no-navigation-computer");
            Log.Warning($"ERT request {request.Id} preflight failed for {call.ID}: no navigation computer.");

            if (loadedForPreflight)
                DiscardPreflightShuttle(request, shuttleUid);

            return false;
        }

        if (!TryFindLandingZone(request, call, computer, out _, out var landingZoneError))
        {
            error = !string.IsNullOrWhiteSpace(landingZoneError)
                ? landingZoneError
                : approving
                ? Loc.GetString("rmc-ert-warning-no-compatible-landing-zone", ("call", call.Name))
                : Loc.GetString("rmc-ert-error-no-landing-zone");

            if (loadedForPreflight)
                DiscardPreflightShuttle(request, shuttleUid);

            return false;
        }

        return true;
    }

    private bool TryEnsureShuttleLoaded(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        out EntityUid? shuttle,
        out bool loadedForPreflight,
        out string error)
    {
        loadedForPreflight = false;
        error = string.Empty;

        if (request.Shuttle is { Valid: true } existing && Exists(existing))
        {
            shuttle = existing;
            return true;
        }

        if (!TryLoadShuttle(request, call, out shuttle, out error))
            return false;

        request.Shuttle = shuttle;
        loadedForPreflight = shuttle is { Valid: true };
        return true;
    }

    private void DiscardPreflightShuttle(RMCERTRequest request, EntityUid shuttle)
    {
        if (request.Shuttle == shuttle)
            request.Shuttle = null;

        request.ShuttleSpawnMarker = null;
        request.ShuttleHomeVisualCoordinates = null;
        request.ShuttleHomeReturnCoordinates = null;
        request.ShuttleHomeRotation = null;
        DeleteReturnDestination(request);

        if (Exists(shuttle))
            QueueDel(shuttle);
    }

    private bool TryLoadShuttle(RMCERTRequest request, RMCERTCallPrototype call, out EntityUid? shuttle, out string error)
    {
        shuttle = null;
        error = string.Empty;

        if (!TryGetShuttleMapPath(call, out var shuttleMap, out error))
            return false;

        if (shuttleMap is not { } shuttleMapPath)
            return true;

        request.ShuttleSpawnMarker = null;
        request.ShuttleHomeVisualCoordinates = null;
        request.ShuttleHomeReturnCoordinates = null;
        request.ShuttleHomeRotation = null;

        if (TryLoadShuttleAtPlacedMarker(request, call, shuttleMapPath, out shuttle, out error) &&
            shuttle is { } placedShuttle)
        {
            ConfigureLoadedShuttle(request, call, placedShuttle);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(error))
            return false;

        if (GetShuttleSpawnMarkerId(call) is { } spawnMarker)
        {
            Log.Warning($"ERT request {request.Id} found no free placed shuttle spawn marker " +
                        $"{spawnMarker.Id} for {call.ID}; falling back to hidden ERT map spawn.");
        }

        if (_ertMap is not { } targetMap || !_map.MapExists(targetMap))
        {
            _map.CreateMap(out var ertMap);
            _ertMap = ertMap;
            targetMap = ertMap;
        }

        // Keep staged ERT shuttles separated on the hidden map so simultaneous dispatches do not overlap.
        var offset = new Vector2(_loadedShuttles * 50, _loadedShuttles * 50);
        _loadedShuttles++;

        if (!_mapLoader.TryLoadGrid(targetMap, shuttleMapPath, out var result, offset: offset) ||
            result == null)
        {
            error = Loc.GetString("rmc-ert-error-load-shuttle-map", ("map", shuttleMapPath));
            return false;
        }

        shuttle = result.Value;
        ConfigureLoadedShuttle(request, call, shuttle.Value);
        return true;
    }

    private bool TryLoadShuttleAtPlacedMarker(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        ResPath shuttleMapPath,
        out EntityUid? shuttle,
        out string error)
    {
        shuttle = null;
        error = string.Empty;

        if (!TryFindShuttleSpawnMarker(request, call, out var marker, out var markerOffset, out var centerShuttleOnMarker))
            return false;

        var markerXform = Transform(marker);
        if (markerXform.MapUid == null)
            return false;

        var coordinates = _transform.GetMapCoordinates(marker, markerXform).Offset(markerOffset);
        var rotation = _transform.GetWorldRotation(markerXform);

        var stagingOffset = new Vector2(_loadedShuttles * 50, _loadedShuttles * 50);
        _loadedShuttles++;
        if (!_mapLoader.TryLoadGrid(markerXform.MapID, shuttleMapPath, out var result, offset: stagingOffset) ||
            result == null)
        {
            error = Loc.GetString("rmc-ert-error-load-shuttle-map", ("map", shuttleMapPath));
            return false;
        }

        shuttle = result.Value;
        var spawnPosition = coordinates.Position;
        if (centerShuttleOnMarker &&
            TryGetShuttleHardBoundsCenter(shuttle.Value, out var hardBoundsCenter))
        {
            spawnPosition -= rotation.RotateVec(hardBoundsCenter);
        }

        if (TryFindShuttleSpawnObstacle(shuttle.Value, marker, markerXform.MapUid.Value, spawnPosition, rotation, out var obstacle))
        {
            Log.Warning($"ERT request {request.Id} could not load shuttle {ToPrettyString(shuttle.Value)} for {call.ID} " +
                        $"at placed marker {ToPrettyString(marker)} because it would intersect {ToPrettyString(obstacle)}. " +
                        $"markerPosition={coordinates.Position}, spawnPosition={spawnPosition}; " +
                        "falling back to hidden ERT map spawn.");

            QueueDel(shuttle.Value);
            shuttle = null;
            return false;
        }

        var shuttleXform = Transform(shuttle.Value);
        _transform.SetCoordinates(shuttle.Value, shuttleXform, new EntityCoordinates(markerXform.MapUid.Value, spawnPosition), rotation);
        request.ShuttleSpawnMarker = marker;
        request.ShuttleHomeVisualCoordinates = coordinates;
        request.ShuttleHomeReturnCoordinates = new MapCoordinates(spawnPosition, markerXform.MapID);
        request.ShuttleHomeRotation = rotation;
        Log.Info($"ERT request {request.Id} loaded shuttle {ToPrettyString(shuttle.Value)} for {call.ID} " +
                 $"at placed marker {ToPrettyString(marker)}.");
        return true;
    }

    private bool TryGetShuttleHardBoundsCenter(EntityUid shuttle, out Vector2 center)
    {
        center = default;

        if (!TryComp(shuttle, out FixturesComponent? shuttleFixtures))
            return false;

        var localTransform = new Transform(Vector2.Zero, Angle.Zero);
        Box2? bounds = null;
        foreach (var fixture in shuttleFixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var fixtureBounds = fixture.Shape.ComputeAABB(localTransform, i);
                bounds = bounds?.Union(fixtureBounds) ?? fixtureBounds;
            }
        }

        if (bounds == null)
            return false;

        center = bounds.Value.Center;
        return true;
    }

    private bool TryFindShuttleSpawnObstacle(
        EntityUid shuttle,
        EntityUid marker,
        EntityUid targetMap,
        Vector2 position,
        Angle rotation,
        out EntityUid obstacle)
    {
        obstacle = default;

        if (!TryComp(shuttle, out FixturesComponent? shuttleFixtures))
            return false;

        var projectedTransform = new Transform(position, rotation);
        foreach (var fixture in shuttleFixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _shuttleSpawnObstacles.Clear();
            _entityLookup.GetLocalEntitiesIntersecting(
                targetMap,
                fixture.Shape,
                projectedTransform,
                _shuttleSpawnObstacles,
                flags: LookupFlags.Uncontained);

            foreach (var ent in _shuttleSpawnObstacles)
            {
                if (ent == shuttle ||
                    ent == marker ||
                    HasComp<AreaComponent>(ent) ||
                    HasComp<FTLSmashImmuneComponent>(ent))
                {
                    continue;
                }

                if (!TryComp(ent, out TransformComponent? entXform) ||
                    entXform.GridUid == shuttle ||
                    !entXform.Anchored)
                {
                    continue;
                }

                if (!TryComp(ent, out PhysicsComponent? physics) ||
                    !physics.CanCollide ||
                    !physics.Hard)
                {
                    continue;
                }

                obstacle = ent;
                return true;
            }
        }

        return false;
    }

    private bool TryFindShuttleSpawnMarker(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        out EntityUid marker,
        out Vector2 markerOffset,
        out bool centerShuttleOnMarker)
    {
        marker = default;
        markerOffset = default;
        centerShuttleOnMarker = false;
        if (GetShuttleSpawnMarkerId(call) is not { } spawnMarker)
            return false;

        var sourceMap = GetRequestSourceMap(request);

        var sourceCandidates = new List<(EntityUid Marker, Vector2 Offset, bool CenterShuttleOnMarker)>();
        var fallbackCandidates = new List<(EntityUid Marker, Vector2 Offset, bool CenterShuttleOnMarker)>();
        var startPadQuery = EntityQueryEnumerator<RMCERTShuttleStartPadComponent, TransformComponent>();
        while (startPadQuery.MoveNext(out var uid, out var startPad, out var xform))
        {
            if (MetaData(uid).EntityPrototype?.ID != spawnMarker.Id)
                continue;

            if (IsShuttleSpawnMarkerReserved(uid))
                continue;

            var targetCandidates = sourceMap != null && xform.MapUid == sourceMap
                ? sourceCandidates
                : fallbackCandidates;
            targetCandidates.Add((uid, startPad.Offset, true));
        }

        var query = EntityQueryEnumerator<GridSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawner, out var xform))
        {
            if (spawner.Spawn == null)
                continue;

            if (MetaData(uid).EntityPrototype?.ID != spawnMarker.Id)
                continue;

            if (IsShuttleSpawnMarkerReserved(uid))
                continue;

            var targetCandidates = sourceMap != null && xform.MapUid == sourceMap
                ? sourceCandidates
                : fallbackCandidates;
            targetCandidates.Add((uid, spawner.Offset, false));
        }

        var availableCandidates = sourceCandidates.Count != 0
            ? sourceCandidates
            : fallbackCandidates;

        if (availableCandidates.Count == 0)
            return false;

        var picked = _random.Pick(availableCandidates);
        marker = picked.Marker;
        markerOffset = picked.Offset;
        centerShuttleOnMarker = picked.CenterShuttleOnMarker;
        return true;
    }

    private static EntProtoId? GetShuttleSpawnMarkerId(RMCERTCallPrototype call)
    {
        return call.ShuttleSpawnMarker ?? call.ShuttleSpawner;
    }

    private bool IsShuttleSpawnMarkerReserved(EntityUid marker)
    {
        foreach (var request in _requests.Values)
        {
            if (request.ShuttleSpawnMarker != marker)
                continue;

            if (IsTerminal(request.State))
            {
                if (request.Shuttle is not { Valid: true } terminalShuttle || !Exists(terminalShuttle))
                    continue;
            }

            return true;
        }

        return false;
    }

    private void ConfigureLoadedShuttle(RMCERTRequest request, RMCERTCallPrototype call, EntityUid shuttle)
    {
        var shuttleComp = EnsureComp<RMCERTShuttleComponent>(shuttle);
        shuttleComp.RequestId = request.Id;
        Dirty(shuttle, shuttleComp);

        CreateReturnDestination(request, shuttle);
        SetShuttlePlayerRouteLock(shuttle, null);

        var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (computerQuery.MoveNext(out var uid, out var computer, out var xform))
        {
            if (xform.GridUid != shuttle)
                continue;

            _dropship.ConfigureRestrictedNavigationComputer((uid, computer),
                !shuttleComp.NoHijack,
                false,
                true,
                call.LandingTags,
                call.DeniedLandingTags);
        }

        ApplyShuttleDoorConsoleLocks(shuttle);
        Log.Info($"ERT request {request.Id} loaded shuttle {ToPrettyString(shuttle)} for {call.ID}.");
    }

    private int ApplyShuttleDoorConsoleLocks(EntityUid shuttle)
    {
        var locked = 0;
        var enumerator = Transform(shuttle).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!HasComp<DockingComponent>(child) ||
                !HasComp<DoorBoltComponent>(child))
            {
                continue;
            }

            EnsureComp<RMCDropshipDoorConsoleLockComponent>(child);
            _dropship.LockDoor(child);
            locked++;
        }

        return locked;
    }

    private bool TryGetReturnHomeFlight(EntityUid shuttle, RMCERTShuttleComponent shuttleComp, out RMCERTRequest request)
    {
        if (_requests.TryGetValue(shuttleComp.RequestId, out request!) &&
            request.State == RMCERTRequestState.Arrived &&
            request.Shuttle == shuttle &&
            request.ShuttleHomeDestination is { } homeDestination &&
            Exists(homeDestination) &&
            TryComp(shuttle, out DropshipComponent? dropship) &&
            dropship.Destination == homeDestination)
        {
            return true;
        }

        request = default!;
        return false;
    }

    private void ReleasePrelaunchShuttleDoorLocks(EntityUid shuttle)
    {
        var enumerator = Transform(shuttle).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RemComp<RMCDropshipDoorConsoleLockComponent>(child);
        }
    }

    private void CreateReturnDestination(RMCERTRequest request, EntityUid shuttle)
    {
        DeleteReturnDestination(request);

        if (!Exists(shuttle))
            return;

        var returnCoordinates = request.ShuttleHomeReturnCoordinates ?? _transform.GetMapCoordinates(shuttle);
        if (returnCoordinates.MapId == MapId.Nullspace)
            return;

        var visualCoordinates = request.ShuttleHomeVisualCoordinates ?? returnCoordinates;
        if (visualCoordinates.MapId == MapId.Nullspace)
            return;

        var rotation = request.ShuttleHomeRotation ?? _transform.GetWorldRotation(shuttle);
        var destination = Spawn(GetReturnDestinationPrototype(shuttle), visualCoordinates);
        _transform.SetWorldRotation(destination, rotation);

        var coordinatesOverride = EnsureComp<RMCDropshipDestinationCoordinatesOverrideComponent>(destination);
        coordinatesOverride.Coordinates = returnCoordinates;
        coordinatesOverride.Rotation = rotation;

        request.ShuttleHomeDestination = destination;
        request.ShuttleHomeIsFallback = request.ShuttleSpawnMarker == null;

        if (TryComp(destination, out DropshipDestinationComponent? destinationComp))
            _dropship.SetDestinationShip((destination, destinationComp), shuttle);

        _dropship.SetCurrentDestination(shuttle, destination);
    }

    private EntProtoId GetReturnDestinationPrototype(EntityUid shuttle)
    {
        if (!TryFindNavigationComputer(shuttle, out var computer))
            return ERTShuttleReturnDestinationPrototype;

        return computer.Comp.ShuttleDockingClass switch
        {
            RMCShuttleDockingClass.Small => ERTShuttleReturnDestinationSmallPrototype,
            RMCShuttleDockingClass.Big => ERTShuttleReturnDestinationBigPrototype,
            _ => ERTShuttleReturnDestinationPrototype,
        };
    }

    private void DeleteReturnDestination(RMCERTRequest request)
    {
        if (request.ShuttleHomeDestination is { Valid: true } destination && Exists(destination))
        {
            if (request.Shuttle is { Valid: true } shuttle && Exists(shuttle))
                _dropship.ClearCurrentDestinationIf(shuttle, destination);

            QueueDel(destination);
        }

        request.ShuttleHomeDestination = null;
        request.ShuttleHomeIsFallback = false;
    }

    private void SetShuttlePlayerRouteLock(EntityUid shuttle, EntityUid? destination)
    {
        _dropship.SetPlayerRouteLock(shuttle, destination);
    }

    private void ClearShuttlePlayerRouteLock(EntityUid shuttle)
    {
        _dropship.ClearPlayerRouteLock(shuttle);
    }

    private void MakeReturnRouteAvailable(RMCERTRequest request)
    {
        if (request.Shuttle is not { Valid: true } shuttle ||
            !Exists(shuttle) ||
            request.ShuttleHomeDestination is not { Valid: true } home ||
            !Exists(home))
            return;

        SetShuttlePlayerRouteLock(shuttle, home);
    }

    private bool TryGetShuttleMapPath(RMCERTCallPrototype call, out ResPath? shuttleMap, out string error)
    {
        shuttleMap = call.ShuttleMap;
        error = string.Empty;

        if (call.ShuttleSpawner is not { } shuttleSpawner)
            return true;

        if (!_prototypes.TryIndex(shuttleSpawner, out var spawnerPrototype))
        {
            error = Loc.GetString("rmc-ert-error-unknown-shuttle-spawner", ("id", shuttleSpawner.Id));
            return false;
        }

        if (!spawnerPrototype.TryGetComponent<GridSpawnerComponent>(out var gridSpawner, _componentFactory))
        {
            error = Loc.GetString("rmc-ert-error-shuttle-spawner-missing-grid", ("id", shuttleSpawner.Id));
            return false;
        }

        if (shuttleMap is null)
            shuttleMap = gridSpawner.Spawn;

        if (shuttleMap is null)
        {
            error = Loc.GetString("rmc-ert-error-shuttle-spawner-no-map", ("id", shuttleSpawner.Id));
            return false;
        }

        return true;
    }
}
