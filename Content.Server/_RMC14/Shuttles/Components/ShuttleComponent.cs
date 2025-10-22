using Robust.Shared.Maths;

namespace Content.Server.Shuttles.Components;

public sealed partial class ShuttleComponent
{
    /// <summary>
    /// Thrust direction applied when entering FTL.
    /// </summary>
    [DataField("FTLDirection")]
    public DirectionFlag FTLDirection = DirectionFlag.North;
}
