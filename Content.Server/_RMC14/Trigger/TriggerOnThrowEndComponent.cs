namespace Content.Server._RMC14.Trigger;

[RegisterComponent]
[Access(typeof(RMCTriggerSystem))]
public sealed partial class TriggerOnThrowEndComponent : Component
{
    [DataField]
    public TimeSpan Delay;
}
