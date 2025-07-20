using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
[EntityCategory("Spawner")]
public sealed partial class AegisSpawnerComponent : Component
{
    [DataField]
    public EntProtoId Spawn = "RMCAegisBox";

    [DataField]
    public bool DeleteAfterSpawn = true;
}
