using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

[Flags]
[Serializable, NetSerializable]
public enum TacticalMapTileFlags : ushort
{
    None = 0,
    Area = 1 << 0,
    CAS = 1 << 1,
    MortarFire = 1 << 2,
    MortarPlacement = 1 << 3,
    Lase = 1 << 4,
    Medevac = 1 << 5,
    Paradrop = 1 << 6,
    OrbitalBombard = 1 << 7,
    SupplyDrop = 1 << 8,
    Fulton = 1 << 9,
    LandingZone = 1 << 10,
}

public sealed class TacticalMapChunk
{
    public const int Size = 16;
    public const int TileCount = Size * Size;

    public readonly uint[] Colors = new uint[TileCount];
    public readonly uint[] Flags = new uint[TileCount];
    public GameTick LastUpdate;

    // low 16 bits are what the area allows, high 16 bits are what roofs block
    public static uint PackFlags(TacticalMapTileFlags area, TacticalMapTileFlags roofBlocked)
    {
        return (uint) area | ((uint) roofBlocked << 16);
    }

    public static TacticalMapTileFlags GetAreaFlags(uint packed)
    {
        return (TacticalMapTileFlags) (packed & 0xFFFF);
    }

    public static TacticalMapTileFlags GetRoofBlocked(uint packed)
    {
        return (TacticalMapTileFlags) (packed >> 16);
    }

    public static TacticalMapTileFlags GetEffectiveFlags(uint packed)
    {
        return GetAreaFlags(packed) & ~GetRoofBlocked(packed);
    }

    public static Vector2i ToChunk(Vector2i tile)
    {
        return new Vector2i(FloorDiv(tile.X), FloorDiv(tile.Y));
    }

    public static int GetIndex(Vector2i tile)
    {
        var x = tile.X - FloorDiv(tile.X) * Size;
        var y = tile.Y - FloorDiv(tile.Y) * Size;
        return y * Size + x;
    }

    // rounds down so negative tiles land in the right chunk
    private static int FloorDiv(int value)
    {
        return value >= 0 ? value / Size : (value - Size + 1) / Size;
    }
}

[Serializable, NetSerializable]
public readonly record struct TacticalMapChunkData(uint[] Colors, uint[] Flags);
