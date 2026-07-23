using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Shuttles;
using Content.Server.Doors.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._RMC14.Elevators;
using Content.Shared._RMC14.Evacuation;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Microsoft.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server._RMC14.Elevators;

public sealed class RMCElevatorSystem : SharedRMCElevatorSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DoorSystem _door = default!;

    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<DoorBoltComponent> _doorBoltsQuery;
    private EntityQuery<RMCElevatorDoorComponent> _elevatorDoorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _doorQuery = GetEntityQuery<DoorComponent>();
        _doorBoltsQuery = GetEntityQuery<DoorBoltComponent>();
        _elevatorDoorQuery = GetEntityQuery<RMCElevatorDoorComponent>();

        SubscribeLocalEvent<RMCElevatorPanelComponent, MapInitEvent>(OnPanelMapInit);
        SubscribeLocalEvent<RMCElevatorDestinationDoorComponent, MapInitEvent>(OnDestDoorMapInit);

        SubscribeLocalEvent<RMCElevatorDestinationComponent, MapInitEvent>(OnDestMapInit);
        SubscribeLocalEvent<RMCElevatorDestinationComponent, SpawnedGridEvent>(OnDestSpawnGrid);

        SubscribeLocalEvent<RMCElevatorComponent, MapInitEvent>(OnElevatorMapInit);
        SubscribeLocalEvent<RMCElevatorComponent, FTLRequestEvent>(OnRefreshBoth);
        SubscribeLocalEvent<RMCElevatorComponent, FTLStartedEvent>(OnStart);
        SubscribeLocalEvent<RMCElevatorComponent, FTLCompletedEvent>(OnComplete);
        SubscribeLocalEvent<RMCElevatorComponent, BeforeFTLStartedEvent>(OnRefreshBoth);
        SubscribeLocalEvent<RMCElevatorComponent, FTLUpdatedEvent>(OnRefreshUpdate);
    }
    private void OnPanelMapInit(Entity<RMCElevatorPanelComponent> ent, ref MapInitEvent args)
    {
        TryLink(ent);
    }

    private void OnDestDoorMapInit(Entity<RMCElevatorDestinationDoorComponent> ent, ref MapInitEvent args)
    {
        TryLink(ent);
    }

    private void OnElevatorMapInit(Entity<RMCElevatorComponent> ent, ref MapInitEvent args)
    {
        SetDoorLocks(ent, false, "");
    }
    private void OnDestMapInit(Entity<RMCElevatorDestinationComponent> ent, ref MapInitEvent args)
    {
        RefreshUI(ent.Comp.ElevatorId, true);
    }

    private void OnDestSpawnGrid(Entity<RMCElevatorDestinationComponent> ent, ref SpawnedGridEvent args)
    {
        var elevator = EnsureComp<RMCElevatorComponent>(args.grid);

        elevator.CurrentDestination = ent;
        Dirty(args.grid, elevator);

        RefreshUI(ent.Comp.ElevatorId, true);
    }

    protected override bool Fly(Entity<RMCElevatorComponent> ent, EntityUid destination, EntityUid? user)
    {
        base.Fly(ent, destination, user);

        if (!TryComp(ent, out ShuttleComponent? shuttleComp))
        {
            Log.Warning($"Tried to launch {ToPrettyString(ent)} outside of a shuttle.");
            return false;
        }

        if (HasComp<FTLComponent>(ent))
        {
            Log.Warning($"Tried to launch shuttle {ToPrettyString(ent)} in FTL");
            return false;
        }

        if (ent.Comp.CurrentDestination == destination)
        {
            Log.Warning($"Tried to launch shuttle {ToPrettyString(ent)} to the same destination");
            return false;
        }

        ent.Comp.CurrentDestination = destination;
        Dirty(ent);

        var destTransform = Transform(destination);
        var destCoords = _transform.GetMoverCoordinates(destination, destTransform);
        var rotation = destTransform.LocalRotation;

        if (TryComp(ent, out PhysicsComponent? physics))
        {
            _physics.SetLocalCenter(ent, physics, Vector2.Zero);
            destCoords = destCoords.Offset(-physics.LocalCenter);
        }

        if (ent.Comp.DestinationOffset != null)
            destCoords = destCoords.Offset(ent.Comp.DestinationOffset.Value);

        var hyperspaceTime = ent.Comp.TravelTime;

        hyperspaceTime += ent.Comp.ArrivalTime;

        _shuttle.FTLToCoordinates(ent, shuttleComp, destCoords, rotation, ent.Comp.StartupTime, hyperspaceTime);

        _adminLog.Add(LogType.RMCDropshipLaunch,
            $"{ToPrettyString(user):player} started sending {ToPrettyString(ent):elevator} to {ToPrettyString(destination):destination}");

        return true;
    }

    private void OnRefreshBoth<T>(Entity<RMCElevatorComponent> ent, ref T args)
    {
        RefreshUI(ent.Comp.ElevatorId, false);
        RefreshUI(ent.Comp.ElevatorId, true);
    }

    private void OnRefreshUpdate(Entity<RMCElevatorComponent> ent, ref FTLUpdatedEvent args)
    {
        if (TryComp(ent, out FTLComponent? ftl) && ftl.State == FTLState.Arriving)
        {
            ftl.StateTime = StartEndTime.FromCurTime(_timing, ent.Comp.ArrivalTime);
            if (ent.Comp.CurrentDestination != null)
            {
                var audio = _audio.PlayPvs(ent.Comp.ArrivalSound, ent.Comp.CurrentDestination.Value);
                if (audio != null)
                {
                    ent.Comp.ArrivalSoundEnt = audio.Value.Entity;
                    Dirty(ent);
                }
            }
        }

        RefreshUI(ent.Comp.ElevatorId, false);
    }

    private void OnStart(Entity<RMCElevatorComponent> ent, ref FTLStartedEvent args)
    {
        SetDoorLocks(ent, true, String.Empty);
        SetDestinationDoors(ent, null);

        RefreshUI(ent.Comp.ElevatorId, false);
        RefreshUI(ent.Comp.ElevatorId, true);
    }

    private void OnComplete(Entity<RMCElevatorComponent> ent, ref FTLCompletedEvent args)
    {
        if (TryComp(ent, out FTLComponent? ftl))
            ftl.StateTime = StartEndTime.FromCurTime(_timing, ent.Comp.CooldownTime);

        QueueDel(ent.Comp.ArrivalSoundEnt);
        var dest = ent.Comp.CurrentDestination != null ? EnsureComp<RMCElevatorDestinationComponent>(ent.Comp.CurrentDestination.Value).OpenDoors : String.Empty;

        SetDoorLocks(ent, false, dest);
        SetDestinationDoors(ent, ent.Comp.CurrentDestination);

        RefreshUI(ent.Comp.ElevatorId, false);
        RefreshUI(ent.Comp.ElevatorId, true);
    }

    protected override void RefreshUI(string elevatorId, bool destinationRefresh)
    {
        var computers = EntityQueryEnumerator<RMCElevatorPanelComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            if (comp.ElevatorId == elevatorId)
                RefreshUI((uid, comp), destinationRefresh);
        }
    }

    protected override void RefreshUI(Entity<RMCElevatorPanelComponent> ent, bool destinationRefresh)
    {
        if (!_ui.IsUiOpen(ent.Owner, ElevatorPanelUiKey.Key))
            return;

        if (!TryGetLinkedElevator(ent, out var elevator))
            return;

        if (destinationRefresh)
        {
            var dests = EntityQueryEnumerator<RMCElevatorDestinationComponent>();

            var destinations = new List<ElevatorDestination>();

            while (dests.MoveNext(out var uid, out var comp))
            {
                var netDestination = GetNetEntity(uid);

                if (comp.ElevatorId == ent.Comp.ElevatorId)
                    destinations.Add(new ElevatorDestination(netDestination, Name(uid)));
            }

            NetEntity? curDest = null;
            if (elevator.Value.Comp.CurrentDestination != null)
                curDest = GetNetEntity(elevator.Value.Comp.CurrentDestination.Value);

            var destState = new ElevatorDestinationsMsg(destinations, curDest);
            _ui.ServerSendUiMessage(ent.Owner, ElevatorPanelUiKey.Key, destState);
            return;
        }

        string destination = Loc.GetString("rmc-elevator-location-unknown");

        StartEndTime stateTime = new StartEndTime();
        FTLState state = FTLState.Available;

        if (TryComp<FTLComponent>(elevator, out var ftl) && ftl.Running)
        {
            state = ftl.State;
            stateTime = ftl.StateTime;
        }

        if (elevator.Value.Comp.CurrentDestination != null)
            destination = Name(elevator.Value.Comp.CurrentDestination.Value);

        var travelState = new ElevatorTravellingMsg(state, stateTime, destination);
        _ui.ServerSendUiMessage(ent.Owner, ElevatorPanelUiKey.Key, travelState);
    }

    private void SetDoorLocks(Entity<RMCElevatorComponent> elevator, bool lockDoors, string openId = "")
    {
        var enumerator = Transform(elevator).ChildEnumerator;

        while (enumerator.MoveNext(out var ent))
        {
            if (!_doorQuery.TryComp(ent, out var door) ||
                !_elevatorDoorQuery.TryComp(ent, out var eleDoor) ||
                !_doorBoltsQuery.TryComp(ent, out var bolts))
                continue;

            if (openId != String.Empty && !eleDoor.OpenAtIds.Contains(openId))
                continue;

            ToggleDoor(ent, door, bolts, lockDoors);
        }
    }

    private void SetDestinationDoors(Entity<RMCElevatorComponent> elevator, EntityUid? destination)
    {
        var query = EntityQueryEnumerator<RMCElevatorDestinationDoorComponent, DoorComponent, DoorBoltComponent>();

        while (query.MoveNext(out var uid, out var dest, out var door, out var bolts))
        {
            if (dest.ElevatorId != elevator.Comp.ElevatorId)
                continue;

            if (dest.LinkedDestination == null)
                continue;

            ToggleDoor(uid, door, bolts, (destination == null || dest.LinkedDestination != destination));
        }
    }

    private void ToggleDoor(EntityUid ent, DoorComponent door, DoorBoltComponent bolts, bool doLock)
    {
        //TODO RMC14 fix bolting so they don't get stuck open/closed somehow
        if (doLock)
        {
            _door.TryClose(ent, door);
            //_door.TrySetBoltDown((ent, bolts), true);
        }
        else
        {
            //_door.TrySetBoltDown((ent, bolts), false);
            _door.TryOpen(ent, door);
        }
    }
}
