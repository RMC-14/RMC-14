using System.Numerics;
using Robust.Shared.Map;

public class PingWaypointData
{
    public EntityUid EntityUid { get; }
    public string PingType { get; }
    public EntityUid Creator { get; }
    public Vector2 WorldPosition { get; set; }
    public EntityCoordinates OriginalCoordinates { get; set; }
    public MapId MapId { get; }
    public Color Color { get; set; }
    public Robust.Client.Graphics.Texture? Texture { get; set; }
    public TimeSpan DeleteAt { get; set; }
    public bool EntityIsLoaded { get; set; }
    public int GroupCount { get; set; } = 1;
    public EntityUid? AttachedTarget { get; set; }
    public bool IsTargetValid { get; set; } = true;
    public bool IsTilePing { get; set; } = false;
    public bool HasStoredPosition { get; set; } = false;

    public bool IsValid => Texture != null;

    public PingWaypointData(EntityUid entityUid, string pingType, EntityUid creator, Vector2 worldPosition,
        EntityCoordinates originalCoordinates, MapId mapId, Color color, Robust.Client.Graphics.Texture? texture,
        TimeSpan deleteAt, EntityUid? attachedTarget = null)
    {
        EntityUid = entityUid;
        PingType = pingType;
        Creator = creator;
        WorldPosition = worldPosition;
        OriginalCoordinates = originalCoordinates;
        MapId = mapId;
        Color = color;
        Texture = texture;
        DeleteAt = deleteAt;
        AttachedTarget = attachedTarget;
        EntityIsLoaded = false;
        IsTargetValid = attachedTarget == null || attachedTarget != EntityUid.Invalid;
        IsTilePing = attachedTarget == null;
        HasStoredPosition = false;
    }
}
