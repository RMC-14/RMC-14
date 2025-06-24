using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
[EntityCategory("Spawner")]
public sealed partial class GunSpawnerComponent : Component
{
    [DataField]
    public int MinMagazines = 1;

    [DataField]
    public int MaxMagazines = 5;

    [DataField]
    public float Offset = 3.0f;

    [DataField]
    public float ChanceToSpawn = 1.0f;

    [DataField]
    public List<(EntProtoId Gun, EntProtoId Ammo)> Prototypes { get; set; } = new();

    [DataField]
    public bool DeleteAfterSpawn = true;
}
