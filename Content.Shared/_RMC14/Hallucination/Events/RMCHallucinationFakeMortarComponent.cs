namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationFakeMortarComponent : Component
{
    [DataField]
    public TimeSpan ImpactWarnDelay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan ImpactDelay = TimeSpan.FromSeconds(4.5);
}
