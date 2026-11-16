using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 cures this with brain surgery
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(RMCNervousSystem))]
public sealed partial class RMCNervousComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCheck;
}
