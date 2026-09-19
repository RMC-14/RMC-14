namespace Content.Shared._RMC14.Basketball;

[RegisterComponent]
[Access(typeof(RMCBasketballSystem))]
public sealed partial class RMCBasketballShotAttemptComponent : Component
{
    public bool Attempted;
    public TimeSpan? ThrownTime;
}
