using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCWaterSystem))]
public sealed partial class ActiveWaterComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SpreadAt;

    /// <summary>
    /// Direction in which the purification wave travelled to reach this node.
    /// Used to throw loose items away from the source of the wave.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 IncomingDirection;

    /// <summary>
    /// Whether synchronized idle animation should be restored after the purification transition.
    /// </summary>
    [DataField]
    public bool RestoreSyncSprite;
}
