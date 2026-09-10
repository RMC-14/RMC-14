using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.SwiftSteps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoSwiftStepsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BaseDodgeThreshold = 6;

    [DataField, AutoNetworkedField]
    public TimeSpan CountingDuration = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public int ProjectilesCounted = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan? CountingExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan IgnoreDuration = TimeSpan.FromSeconds(0.5);

    //Used to prevent the same bullet being counted multiple times in a small span of time
    [DataField, AutoNetworkedField]
    public Dictionary<NetEntity, TimeSpan> IgnoreBullets = new();
}
