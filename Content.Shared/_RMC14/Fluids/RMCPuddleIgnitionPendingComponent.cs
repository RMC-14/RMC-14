using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Fluids;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RMCPuddleFireSystem))]
public sealed partial class RMCPuddleIgnitionPendingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan IgniteAt;
}
