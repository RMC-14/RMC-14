using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Xenonids;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(XenoRoleSystem))]
public sealed partial class XenoDisconnectedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan At;
}
