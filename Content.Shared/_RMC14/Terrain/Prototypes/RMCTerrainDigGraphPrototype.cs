using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Terrain.Prototypes;

[Prototype("rmcTerrainDigGraph"), Serializable, NetSerializable]
public sealed partial class RMCTerrainDigGraphPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<RMCTerrainDigStage> Stages = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCTerrainDigStage
{
    [DataField(required: true)]
    public string Tile = string.Empty;

    [DataField]
    public string? DigTo;

    [DataField]
    public string? PlaceTo;

    [DataField]
    public RMCTerrainMaterial Material = RMCTerrainMaterial.None;

    [DataField]
    public int Yield = 1;

    [DataField]
    public bool Diggable = true;
}
