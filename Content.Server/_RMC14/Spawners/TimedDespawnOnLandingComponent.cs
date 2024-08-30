namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class TimedDespawnOnLandingComponent : Component
{
    [DataField]
    public float Lifetime = 1200;
}
