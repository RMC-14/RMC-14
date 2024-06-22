using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenonids.Health;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoHealthSystem))]
public sealed partial class XenoTimeHealthComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromMinutes(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ChangeAt;

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 CritThreshold;

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 DeadThreshold;

    [DataField, AutoNetworkedField]
    public string NamePrefix = "Immature";
}
