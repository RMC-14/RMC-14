namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationFakeOrbitalStrikeComponent : Component
{
    [DataField]
    public TimeSpan AlertDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan TravelDelay = TimeSpan.FromSeconds(12);

    [DataField]
    public TimeSpan WarnTwoDelay = TimeSpan.FromSeconds(16);

    [DataField]
    public TimeSpan WarnThreeDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan ImpactDelay = TimeSpan.FromSeconds(24);

    [DataField]
    public int Blasts = 5;
}
