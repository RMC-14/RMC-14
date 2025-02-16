using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Maturing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoMaturingSystem))]
public sealed partial class XenoMaturingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromMinutes(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan MatureAt;

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 CritThreshold;

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 DeadThreshold;

    [DataField]
    public ComponentRegistry AddComponents = new();

    [DataField, AutoNetworkedField]
    public List<EntProtoId> AddActions = new();

    [DataField, AutoNetworkedField]
    public string BaseName = string.Empty;
}
