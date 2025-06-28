using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
[EntityCategory("Spawner")]
public sealed partial class ItemPoolSpawnerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Id;

    [DataField]
    public int Quota = 1;

    [DataField]
    public List<EntProtoId> Prototypes { get; set; } = new();
}
