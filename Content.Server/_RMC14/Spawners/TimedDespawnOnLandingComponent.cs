namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class TimedDespawnOnLandingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StartDespawnAt = TimeSpan.FromMinutes(20);

    [DataField]
    public float Lifetime = 1200;
}
