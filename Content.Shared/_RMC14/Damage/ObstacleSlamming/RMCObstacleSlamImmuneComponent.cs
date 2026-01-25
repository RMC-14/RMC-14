using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Damage.ObstacleSlamming;

/// <summary>
/// Makes a mob immune to obstacle slamming for a certain period of time.
/// </summary>
[Access(typeof(RMCObstacleSlammingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCObstacleSlamImmuneComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpireIn = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ExpireAt;
}
