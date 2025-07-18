using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.MapInsert;

[RegisterComponent, EntityCategory("Spawner")]
[Access(typeof(MapInsertSystem))]
public sealed partial class MapInsertComponent : Component
{
    [DataField]
    public bool ClearEntities;

    [DataField]
    public bool ClearDecals;

    [DataField]
    public bool ReplaceAreas;

    [DataField(required: true)]
    public List<SpawnVariation> Variations = new();
}

[DataDefinition]
public sealed partial record SpawnVariation
{
    [DataField(required: true)]
    public ResPath Spawn;

    [DataField]
    public string NightmareScenario = string.Empty;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public float Probability = 1.0f;
}
