using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Xenonids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(XenoRoleSystem))]
public sealed partial class XenoDisconnectedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan At;
}
