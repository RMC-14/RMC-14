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

    public short GetAlertSeverity(EntityUid ent, MapCoordinates tracked)
    {
        var pos = _transform.GetMapCoordinates(ent);
        if (pos.MapId != tracked.MapId)
            return CenterSeverity;

        var vec = tracked.Position - pos.Position;
        return vec.Length() < 1
            ? CenterSeverity
            : AlertSeverity.GetValueOrDefault(vec.ToWorldAngle().GetDir(), CenterSeverity);
    }
}
