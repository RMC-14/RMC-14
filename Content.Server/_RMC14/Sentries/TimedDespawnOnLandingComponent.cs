namespace Content.Server._RMC14.Sentries;

[RegisterComponent]
[Access(typeof(RMCSentrySystem))]
public sealed partial class TimedDespawnOnLandingComponent : Component
{
    [DataField]
    public float Lifetime = 1200;
}
