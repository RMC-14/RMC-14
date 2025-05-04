using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Shared._RMC14.Xenonids.Construction;

// Component granting xeno structures extra temporary hitpoints, e.g. via gardener's resin surge
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoConstructReinforceSystem))]
public sealed partial class XenoConstructReinforceComponent : Component
{
    // Duration of effect
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    // Effect end time
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? EndAt;

    // Amount of temporary hitpoints left
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReinforceAmount = 0;
}
