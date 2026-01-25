using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class CorpseSpawnerComponent : Component
{
    [DataField(required: true)]
    public ProtoId<RandomHumanoidSettingsPrototype>? Spawn;

    [DataField, AutoNetworkedField]
    public bool SkipLimit;
}
