using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Access.Systems;
using Content.Shared.Directions;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Doors;

public sealed class CMDoorSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedDoorSystem _doors = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCPowerSystem _rmcPower = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<CMDoubleDoorComponent> _doubleQuery;

    public override void Initialize()
    {
        _doorQuery = GetEntityQuery<DoorComponent>();
        _doubleQuery = GetEntityQuery<CMDoubleDoorComponent>();

        // TODO RMC14 there is an edge case where one door can close but the other can't, to fix this CanClose should be checked on the adjacent door when a double door tries to close
        SubscribeLocalEvent<CMDoubleDoorComponent, DoorStateChangedEvent>(OnDoorStateChanged);

        SubscribeLocalEvent<RMCDoorButtonComponent, ActivateInWorldEvent>(OnButtonActivateInWorld);

        SubscribeLocalEvent<RMCPodDoorComponent, BeforePryEvent>(OnPodDoorBeforePry);
    }

    private void OnDoorStateChanged(Entity<CMDoubleDoorComponent> door, ref DoorStateChangedEvent args)
    {
        switch (args.State)
        {
            case DoorState.Opening:
                Open(door);
                break;
            case DoorState.Closing:
                Close(door);
                break;
        }
    }

    private void OnButtonActivateInWorld(Entity<RMCDoorButtonComponent> button, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        if (HasComp<XenoComponent>(user))
            return;

        if (!_accessReader.IsAllowed(user, button))
        {
            // TODO RMC14 denied animation
            _popup.PopupClient(Loc.GetString("cm-vending-machine-access-denied"), button, user, PopupType.SmallCaution);
            return;
        }

        var time = _timing.CurTime;
        if (time < button.Comp.LastUse + button.Comp.Cooldown)
            return;

        button.Comp.LastUse = time;
        var buttonName = button.Comp.Id ?? Name(button);
        var buttonTransform = Transform(button);

        var doors = EntityQueryEnumerator<RMCPodDoorComponent, DoorComponent, TransformComponent, MetaDataComponent>();
        while (doors.MoveNext(out var door, out var podDoor, out var doorComp, out var doorTransform, out var metaData))
        {
            if (TerminatingOrDeleted(door))
                continue;

            if (buttonTransform.MapID != doorTransform.MapID)
                continue;

            var id = podDoor.Id ?? metaData.EntityName;
            if (buttonName != id)
                continue;

            if (doorComp.State == DoorState.Open)
            {
                _doors.StartClosing(door);
            }
            else
            {
                _doors.TryOpen(door, doorComp);
            }
        }

        if (_net.IsServer)
            RaiseNetworkEvent(new RMCPodDoorButtonPressedEvent(GetNetEntity(button)), Filter.PvsExcept(button));
    }

    private void OnPodDoorBeforePry(Entity<RMCPodDoorComponent> ent, ref BeforePryEvent args)
    {
        if (TryComp(ent, out DoorComponent? door) && door.State != DoorState.Closed)
        {
            args.Cancelled = true;
            return;
        }

        if (_rmcPower.IsPowered(ent))
            args.Cancelled = true;
    }

    private AnchoredEntitiesEnumerator? GetAdjacentEnumerator(Entity<CMDoubleDoorComponent> ent)
    {
        if (!TryComp(ent, out TransformComponent? transform) ||
            !TryComp(transform.GridUid, out MapGridComponent? grid))
        {
            return default;
        }

        var adjacent = transform.Coordinates.Offset(transform.LocalRotation.GetCardinalDir());
        var position = _map.LocalToTile(transform.GridUid.Value, grid, adjacent);
        return _map.GetAnchoredEntitiesEnumerator(transform.GridUid.Value, grid, position);
    }

    private bool AreFacing(EntityUid one, EntityUid two)
    {
        return TryComp(one, out TransformComponent? transformOne) &&
               TryComp(two, out TransformComponent? transformTwo) &&
               transformOne.LocalRotation.GetCardinalDir().GetOpposite() ==
               transformTwo.LocalRotation.GetCardinalDir();
    }

    private void Open(Entity<CMDoubleDoorComponent> ent)
    {
        if (GetAdjacentEnumerator(ent) is not { } enumerator)
            return;

        var time = _timing.CurTime;

        ent.Comp.LastOpeningAt = time;
        Dirty(ent);

        while (enumerator.MoveNext(out var anchored))
        {
            if (_doubleQuery.TryGetComponent(anchored, out var doubleDoor) &&
                doubleDoor.LastOpeningAt != time &&
                AreFacing(ent, anchored.Value) &&
                _doorQuery.TryGetComponent(anchored, out var door) &&
                door.State != DoorState.Opening)
            {
                doubleDoor.LastOpeningAt = time;
                Dirty(anchored.Value, doubleDoor);

                var sound = door.OpenSound;
                door.OpenSound = null;
                door.Partial = false;
                _doors.StartOpening(anchored.Value, door);
                door.OpenSound = sound;
            }
        }
    }

    private void Close(Entity<CMDoubleDoorComponent> ent)
    {
        if (GetAdjacentEnumerator(ent) is not { } enumerator)
            return;

        var time = _timing.CurTime;

        ent.Comp.LastClosingAt = time;
        Dirty(ent);

        while (enumerator.MoveNext(out var anchored))
        {
            if (_doubleQuery.TryGetComponent(anchored, out var doubleDoor) &&
                doubleDoor.LastClosingAt != time &&
                AreFacing(ent, anchored.Value) &&
                _doorQuery.TryGetComponent(anchored, out var door) &&
                door.State != DoorState.Closing)
            {
                doubleDoor.LastClosingAt = time;
                Dirty(anchored.Value, doubleDoor);

                var sound = door.CloseSound;
                door.CloseSound = null;
                door.Partial = false;
                _doors.StartClosing(anchored.Value, door);
                door.CloseSound = sound;
            }
        }
    }
}
