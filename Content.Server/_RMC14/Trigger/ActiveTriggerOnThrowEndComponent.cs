using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Trigger;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RMCTriggerSystem))]
public sealed partial class ActiveTriggerOnThrowEndComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan TriggerAt;
}
