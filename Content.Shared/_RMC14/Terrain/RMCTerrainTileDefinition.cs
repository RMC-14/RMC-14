using Content.Shared._RMC14.Terrain;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Maps;

public sealed partial class ContentTileDefinition
{
    [DataField]
    public RMCTerrainMaterial RmcDigType = RMCTerrainMaterial.None;

    [DataField]
    public string? RmcTerrainLayerSet;

    [DataField]
    public int RmcTerrainLayer;

    [DataField]
    public float RmcSnowSlowSeconds;

    [DataField]
    public float RmcSnowSuperSlowSeconds;

    [DataField]
    public float RmcSnowSuperSlowChance;
}
