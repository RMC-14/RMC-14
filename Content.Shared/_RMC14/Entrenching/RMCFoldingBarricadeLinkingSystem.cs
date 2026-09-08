using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Directions;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Entrenching;

public sealed class RMCFoldingBarricadeLinkingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly Direction[] CardinalDirections =
    [
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    ];

    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<RMCFoldingBarricadeLinkingComponent> _linkingQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private bool _propagating;

    public override void Initialize()
    {
        _doorQuery = GetEntityQuery<DoorComponent>();
        _linkingQuery = GetEntityQuery<RMCFoldingBarricadeLinkingComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, InteractUsingEvent>(OnInteractUsing,
            before: [typeof(PryingSystem)]);
        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<RMCFoldingBarricadeLinkingComponent, MoveEvent>(OnMove);
    }

    private void OnStartup(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref ComponentStartup args)
    {
        UpdateSelfAndNearby(ent);
    }

    private void OnShutdown(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Linked = false;
        UpdateSelfAndNearby(ent);
    }

    private void OnInteractUsing(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp(args.Used, out PryingComponent? prying) ||
            !prying.Enabled)
        {
            return;
        }

        if (!_transformQuery.TryGetComponent(ent, out var xform) ||
            !xform.Anchored)
        {
            return;
        }

        if (!ent.Comp.Linkable)
        {
            _popup.PopupClient(Loc.GetString("rmc-folding-barricade-link-no-points", ("barricade", ent.Owner)),
                ent,
                args.User,
                PopupType.SmallCaution);
            return;
        }

        if (!_skills.HasSkill(args.User, ent.Comp.Skill, ent.Comp.RequiredSkillLevel))
        {
            _popup.PopupClient(Loc.GetString("rmc-skills-no-training", ("target", ent.Owner)),
                ent,
                args.User,
                PopupType.SmallCaution);
            return;
        }

        args.Handled = true;

        ent.Comp.Linked = !ent.Comp.Linked;
        Dirty(ent);

        var message = ent.Comp.Linked
            ? "rmc-folding-barricade-link-set"
            : "rmc-folding-barricade-link-removed";

        _popup.PopupPredicted(Loc.GetString(message, ("barricade", ent.Owner)), ent, args.User);
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.User);
        UpdateSelfAndNearby(ent);
    }

    private void OnDoorStateChanged(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref DoorStateChangedEvent args)
    {
        if (!_propagating)
            PropagateDoorState(ent, args.State);

        UpdateSelfAndNearby(ent);
    }

    private void OnAnchorStateChanged(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref AnchorStateChangedEvent args)
    {
        UpdateSelfAndNearby(ent);
    }

    private void OnMove(Entity<RMCFoldingBarricadeLinkingComponent> ent, ref MoveEvent args)
    {
        if (!_transformQuery.TryGetComponent(ent, out var xform) ||
            !xform.Anchored)
        {
            UpdateVisuals(ent);
            return;
        }

        UpdateNearby(args.OldPosition);
        UpdateNearby(args.NewPosition);
        UpdateVisuals(ent);
    }

    private void PropagateDoorState(Entity<RMCFoldingBarricadeLinkingComponent> ent, DoorState state)
    {
        if (state is not (DoorState.Opening or DoorState.Closing or DoorState.Open or DoorState.Closed) ||
            !CanLink(ent))
        {
            return;
        }

        if (!_transformQuery.TryGetComponent(ent, out var xform))
            return;

        var facing = xform.LocalRotation.GetCardinalDir();
        var visited = new HashSet<EntityUid> { ent.Owner };

        _propagating = true;
        try
        {
            foreach (var direction in CardinalDirections)
            {
                if (IsRowDirection(facing, direction))
                    PropagateInDirection(ent.Owner, direction, state, visited);
            }
        }
        finally
        {
            _propagating = false;
        }
    }

    private void PropagateInDirection(EntityUid current, Direction direction, DoorState state, HashSet<EntityUid> visited)
    {
        if (!TryGetPropagationNeighbor(current, direction, state, out var neighbor))
            return;

        if (!visited.Add(neighbor))
            return;

        if (ApplyDoorState(neighbor, state))
            PropagateInDirection(neighbor, direction, state, visited);
    }

    private bool TryGetPropagationNeighbor(EntityUid current, Direction direction, DoorState state, out EntityUid neighbor)
    {
        neighbor = default;

        if (!_linkingQuery.TryGetComponent(current, out var linking) ||
            !_doorQuery.TryGetComponent(current, out var door) ||
            !_transformQuery.TryGetComponent(current, out var xform) ||
            !CanLink((current, linking), xform))
        {
            return false;
        }

        var opening = state is DoorState.Opening or DoorState.Open;
        foreach (var candidate in GetAdjacentLinkableBarricades((current, linking), xform, door, direction, checkDoorState: false))
        {
            if (!_doorQuery.TryGetComponent(candidate, out var candidateDoor))
                continue;

            if (opening && candidateDoor.State != DoorState.Closed)
                continue;

            if (!opening && candidateDoor.State != DoorState.Open)
                continue;

            neighbor = candidate;
            return true;
        }

        return false;
    }

    private bool ApplyDoorState(EntityUid uid, DoorState state)
    {
        if (!_doorQuery.TryGetComponent(uid, out var door))
            return false;

        if (state is DoorState.Opening or DoorState.Open)
        {
            if (door.State is DoorState.Open or DoorState.Opening)
                return false;

            var sound = door.OpenSound;
            door.OpenSound = null;
            _door.StartOpening(uid, door);
            door.OpenSound = sound;
            return true;
        }

        if (door.State is DoorState.Closed or DoorState.Closing)
            return false;

        var closeSound = door.CloseSound;
        door.CloseSound = null;
        _door.StartClosing(uid, door);
        door.CloseSound = closeSound;
        return true;
    }

    private void UpdateSelfAndNearby(Entity<RMCFoldingBarricadeLinkingComponent> ent)
    {
        if (_transformQuery.TryGetComponent(ent, out var xform))
            UpdateNearby(xform.Coordinates);

        UpdateVisuals(ent);
    }

    private void UpdateNearby(EntityCoordinates coordinates)
    {
        if (_transform.GetGrid(coordinates) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return;
        }

        UpdateAnchoredAt((gridUid, grid), coordinates);

        foreach (var direction in CardinalDirections)
        {
            UpdateAnchoredAt((gridUid, grid), coordinates.Offset(direction));
        }
    }

    private void UpdateAnchoredAt(Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        var tile = _map.LocalToTile(grid, grid, coordinates);
        var anchored = _map.GetAnchoredEntitiesEnumerator(grid, grid, tile);
        while (anchored.MoveNext(out var anchoredUid))
        {
            if (_linkingQuery.TryGetComponent(anchoredUid, out var linking))
                UpdateVisuals((anchoredUid.Value, linking));
        }
    }

    private void UpdateVisuals(Entity<RMCFoldingBarricadeLinkingComponent> ent)
    {
        _appearance.SetData(ent, RMCFoldingBarricadeLinkingVisuals.North, GetVisualState(ent, Direction.North));
        _appearance.SetData(ent, RMCFoldingBarricadeLinkingVisuals.South, GetVisualState(ent, Direction.South));
        _appearance.SetData(ent, RMCFoldingBarricadeLinkingVisuals.East, GetVisualState(ent, Direction.East));
        _appearance.SetData(ent, RMCFoldingBarricadeLinkingVisuals.West, GetVisualState(ent, Direction.West));
    }

    private RMCFoldingBarricadeLinkingVisualState GetVisualState(Entity<RMCFoldingBarricadeLinkingComponent> ent, Direction direction)
    {
        if (!_doorQuery.TryGetComponent(ent, out var door) ||
            !_transformQuery.TryGetComponent(ent, out var xform) ||
            !CanLink(ent, xform))
        {
            return RMCFoldingBarricadeLinkingVisualState.None;
        }

        foreach (var _ in GetAdjacentLinkableBarricades(ent, xform, door, direction, checkDoorState: true))
        {
            return GetVisualState(door.State);
        }

        return RMCFoldingBarricadeLinkingVisualState.None;
    }

    private IEnumerable<EntityUid> GetAdjacentLinkableBarricades(
        Entity<RMCFoldingBarricadeLinkingComponent> ent,
        TransformComponent xform,
        DoorComponent door,
        Direction direction,
        bool checkDoorState)
    {
        if (!CanLink(ent, xform) ||
            !IsRowDirection(xform.LocalRotation.GetCardinalDir(), direction) ||
            xform.GridUid is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            yield break;
        }

        var facing = xform.LocalRotation.GetCardinalDir();
        var adjacent = xform.Coordinates.Offset(direction);
        var tile = _map.LocalToTile(gridUid, grid, adjacent);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var anchoredUid))
        {
            if (anchoredUid == ent.Owner ||
                !_linkingQuery.TryGetComponent(anchoredUid, out var linking) ||
                !_doorQuery.TryGetComponent(anchoredUid, out var otherDoor) ||
                !_transformQuery.TryGetComponent(anchoredUid, out var otherXform) ||
                !CanLink((anchoredUid.Value, linking), otherXform) ||
                otherXform.LocalRotation.GetCardinalDir() != facing)
            {
                continue;
            }

            if (checkDoorState && GetVisualState(door.State) != GetVisualState(otherDoor.State))
                continue;

            yield return anchoredUid.Value;
        }
    }

    private static bool CanLink(Entity<RMCFoldingBarricadeLinkingComponent> ent)
    {
        return ent.Comp.Linked && ent.Comp.Linkable;
    }

    private static bool CanLink(Entity<RMCFoldingBarricadeLinkingComponent> ent, TransformComponent xform)
    {
        return CanLink(ent) && xform.Anchored;
    }

    private static bool IsRowDirection(Direction facing, Direction direction)
    {
        return facing switch
        {
            Direction.North or Direction.South => direction is Direction.East or Direction.West,
            Direction.East or Direction.West => direction is Direction.North or Direction.South,
            _ => false,
        };
    }

    private static RMCFoldingBarricadeLinkingVisualState GetVisualState(DoorState state)
    {
        return state is DoorState.Open or DoorState.Closing
            ? RMCFoldingBarricadeLinkingVisualState.Open
            : RMCFoldingBarricadeLinkingVisualState.Closed;
    }
}
