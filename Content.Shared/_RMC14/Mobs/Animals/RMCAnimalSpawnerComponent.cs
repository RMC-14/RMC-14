using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCAnimalSpawnerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype = default;

    [DataField]
    public float InitialChance = 0.66f;

    [DataField]
    public int MaxAlive = 4;

    [DataField]
    public TimeSpan LateSpawnMin = TimeSpan.FromMinutes(35);

    [DataField]
    public TimeSpan LateSpawnMax = TimeSpan.FromMinutes(50);

    [DataField]
    public TimeSpan RetryMin = TimeSpan.FromMinutes(15);

    [DataField]
    public TimeSpan RetryMax = TimeSpan.FromMinutes(25);

    [DataField]
    public float WitnessRange = 7f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextLateSpawnAt;
}
