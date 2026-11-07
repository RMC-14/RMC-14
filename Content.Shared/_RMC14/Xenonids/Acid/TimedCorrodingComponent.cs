using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoAcidSystem))]
public sealed partial class TimedCorrodingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Acid;

    [DataField, AutoNetworkedField]
    public EntProtoId AcidPrototype;

    [DataField, AutoNetworkedField]
    public XenoAcidStrength Strength = XenoAcidStrength.Normal;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan CorrodesAt;

    [DataField, AutoNetworkedField]
    public float Dps;

    [DataField, AutoNetworkedField]
    public float LightDps;
}
