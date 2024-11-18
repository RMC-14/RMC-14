using Content.Shared.Movement.Components;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Tracker;

public sealed class TrackerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public static readonly short CenterSeverity = 1;
    private static readonly Dictionary<Direction, short> AlertSeverity = new()
    {
        { Direction.Invalid, 0 },
        { Direction.South, 2 },
        { Direction.SouthEast, 3 },
        { Direction.East, 4 },
        { Direction.NorthEast, 5 },
        { Direction.North, 6 },
        { Direction.NorthWest, 7 },
        { Direction.West, 8 },
        { Direction.SouthWest, 9 },
    };

    private EntityQuery<InputMoverComponent> _inputMoverQuery;

    public override void Initialize()
    {
        base.Initialize();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
    }

    public short GetAlertSeverity(EntityUid ent, MapCoordinates tracked)
    {
        var pos = _transform.GetMapCoordinates(ent);
        if (pos.MapId != tracked.MapId)
            return CenterSeverity;

        var vec = tracked.Position - pos.Position;
        if (vec.Length() < 1)
            return CenterSeverity;

        if (_inputMoverQuery.TryComp(ent, out var inputMover) &&
            inputMover.RelativeRotation != Angle.Zero)
        {
            vec = (-inputMover.RelativeRotation).RotateVec(vec);
        }

        var dir = vec.ToWorldAngle().GetDir();
        return AlertSeverity.GetValueOrDefault(dir, CenterSeverity);
    }
}
