using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(AegisCorpseSpawnerSystem))]
[EntityCategory("Spawner")]
public sealed partial class AegisCorpseSpawnerComponent : Component
{
    [DataField]
    public ProtoId<RandomHumanoidSettingsPrototype> Spawn = "RMCCorpseScientistAegis";

    [DataField]
    public bool DeleteAfterSpawn = true;
}
