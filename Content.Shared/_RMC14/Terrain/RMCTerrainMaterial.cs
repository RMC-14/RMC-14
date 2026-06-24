using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Terrain;

[Serializable, NetSerializable]
public enum RMCTerrainMaterial
{
    None,
    Dirt,
    Mars,
    Snow,
    Sand,
    Shale,
}

public static class RMCTerrainMaterialExtensions
{
    public static bool CanFillSandbags(this RMCTerrainMaterial material)
    {
        return material is RMCTerrainMaterial.Dirt or RMCTerrainMaterial.Mars or RMCTerrainMaterial.Snow or RMCTerrainMaterial.Sand or RMCTerrainMaterial.Shale;
    }
}
