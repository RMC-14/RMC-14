using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Ping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoPingEntityComponent : Component
{
    [DataField, AutoNetworkedField]
    public string PingType = "XenoPingMove";

    [DataField, AutoNetworkedField]
    public EntityUid Creator;

    [DataField, AutoNetworkedField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DeleteAt;
}
