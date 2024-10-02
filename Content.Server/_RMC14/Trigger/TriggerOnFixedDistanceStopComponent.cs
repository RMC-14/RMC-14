using Robust.Shared.GameStates;

namespace Content.Server._RMC14.Trigger;

[RegisterComponent]
[Access(typeof(RMCTriggerSystem))]
public sealed partial class TriggerOnFixedDistanceStopComponent : Component
{
    [DataField]
    public TimeSpan Delay;
}
