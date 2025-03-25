using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class CorpseSpawnerComponent : Component
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>)), AutoNetworkedField]
    public string Spawn;
}
