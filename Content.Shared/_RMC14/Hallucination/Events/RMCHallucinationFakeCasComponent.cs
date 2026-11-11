namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationFakeCasComponent : Component
{
    [DataField]
    public TimeSpan FirstShotDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan ShotDelay = TimeSpan.FromSeconds(0.1);

    [DataField]
    public TimeSpan ImpactTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public int Shots = 10;

    [DataField]
    public int SoundEveryShots = 3;

    [DataField]
    public float Spread = 3;
}
