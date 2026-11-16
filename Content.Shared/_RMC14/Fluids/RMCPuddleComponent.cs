namespace Content.Shared._RMC14.Fluids;

[RegisterComponent]
[Access(typeof(RMCPuddleFireSystem))]
public sealed partial class RMCPuddleComponent : Component
{
    [DataField]
    public TimeSpan IgniteSpreadDelay = TimeSpan.FromMilliseconds(400);

    [DataField]
    public float MinSpreadAmount = 7.5f;
}
