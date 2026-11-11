namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationFakeGrenadeComponent : Component
{
    [DataField]
    public TimeSpan ThrowTime = TimeSpan.FromSeconds(0.4);

    [DataField]
    public TimeSpan Fuse = TimeSpan.FromSeconds(4);
}
