using Content.Shared.Directions;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Doors;

public sealed class CMDoorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doors = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<CMDoubleDoorComponent> _doubleQuery;

    public override void Initialize()
    {
        _doorQuery = GetEntityQuery<DoorComponent>();
        _doubleQuery = GetEntityQuery<CMDoubleDoorComponent>();

        SubscribeLocalEvent<CMDoubleDoorComponent, DoorStateChangedEvent>(OnDoorStateChanged);
    }

    private void OnDoorStateChanged(Entity<CMDoubleDoorComponent> ent, ref DoorStateChangedEvent args)
    {
        switch (args.State)
        {
            case DoorState.Opening:
                Open(ent);
                break;
            case DoorState.Closing:
                Close(ent);
                break;
        }
    }

    private AnchoredEntitiesEnumerator GetAdjacentEnumerator(Entity<CMDoubleDoorComponent> ent)
    {
        if (!TryComp(ent, out TransformComponent? transform) ||
            !TryComp(transform.GridUid, out MapGridComponent? grid))
        {
            return default!;
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
        var enumerator = GetAdjacentEnumerator(ent);
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
                doubleDoor.LastClosingAt = time;
                Dirty(anchored.Value, doubleDoor);
                _doors.StartOpening(anchored.Value);
            }
        }
    }

    private void Close(Entity<CMDoubleDoorComponent> ent)
    {
        var enumerator = GetAdjacentEnumerator(ent);
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
                _doors.StartClosing(anchored.Value);
            }
        }
    }
}
