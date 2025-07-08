using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Damage.ObstacleSlamming;

/// <summary>
/// Gives a mob extra damage or stun on obstacle slam when this component is available.
/// Useful for things like xeno abilities which stun when an ability causes obstacle slams.
/// </summary>
[Access(typeof(RMCObstacleSlammingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCObstacleSlamBonusEffectsComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpireIn = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan Stun = TimeSpan.FromSeconds(0);

    [DataField, AutoNetworkedField]
    public TimeSpan Slow = TimeSpan.FromSeconds(0);
}
