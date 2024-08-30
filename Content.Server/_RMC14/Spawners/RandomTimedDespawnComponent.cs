namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class RandomTimedDespawnComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Min;

    [DataField]
    public TimeSpan Max;
}
