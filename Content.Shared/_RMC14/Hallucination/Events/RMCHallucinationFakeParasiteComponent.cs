namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationFakeParasiteComponent : Component
{
    [DataField]
    public float RunSpeed = 5;

    [DataField]
    public float LeapRange = 2;

    [DataField]
    public TimeSpan LeapDelay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan LeapTime = TimeSpan.FromSeconds(0.35);
}
