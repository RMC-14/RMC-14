using Content.Shared._RMC14.TacticalMap;

namespace Content.Client._RMC14.TacticalMap;

public readonly record struct TacticalMapAreaInfo(
    Vector2i Indices,
    string AreaName,
    string? AreaId,
    string? AreaLabel,
    string? TacticalLabel,
    TacticalMapTileFlags AreaFlags,
    TacticalMapTileFlags RoofBlocked,
    string? LinkedLz)
{
    public bool HasArea => (AreaFlags & TacticalMapTileFlags.Area) != 0;

    public TacticalMapTileFlags Effective => AreaFlags & ~RoofBlocked;

    public bool Allows(TacticalMapTileFlags flag)
    {
        return (Effective & flag) != 0;
    }
}

public static class TacticalMapRoofLevels
{
    public const int Count = 5;

    public static int GetLevel(TacticalMapTileFlags effective)
    {
        if ((effective & TacticalMapTileFlags.Area) == 0)
            return -1;

        if ((effective & TacticalMapTileFlags.OrbitalBombard) == 0)
            return 4;

        if ((effective & TacticalMapTileFlags.CAS) == 0)
            return 3;

        if ((effective & TacticalMapTileFlags.SupplyDrop) == 0 || (effective & TacticalMapTileFlags.MortarFire) == 0)
            return 2;

        const TacticalMapTileFlags roofed = TacticalMapTileFlags.MortarPlacement |
                                            TacticalMapTileFlags.Lase |
                                            TacticalMapTileFlags.Medevac |
                                            TacticalMapTileFlags.Paradrop;
        if ((effective & roofed) != roofed)
            return 1;

        return 0;
    }

    public static string GetState(int level)
    {
        return level switch
        {
            0 => "roof0",
            1 => "roof1",
            2 => "roof2",
            3 => "roof3",
            4 => "roof4",
            _ => "roofnull",
        };
    }

    public static Color GetOverlayColor(int level)
    {
        return level switch
        {
            0 => Color.FromHex("#3DBF75").WithAlpha(0.24f),
            1 => Color.FromHex("#A6D96A").WithAlpha(0.24f),
            2 => Color.FromHex("#F4B860").WithAlpha(0.24f),
            3 => Color.FromHex("#F28C7A").WithAlpha(0.24f),
            4 => Color.FromHex("#6A61FF").WithAlpha(0.24f),
            _ => Color.Transparent,
        };
    }
}

public static class TacticalMapLinkedLz
{
    public static Color GetColor(string linkedLz)
    {
        var count = 0;
        var r = 0f;
        var g = 0f;
        var b = 0f;
        var maxAlpha = 0f;
        foreach (var part in linkedLz.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var id = part.Trim();
            if (id.Length == 0)
                continue;

            var color = GetSingleColor(id);
            r += color.R;
            g += color.G;
            b += color.B;
            maxAlpha = MathF.Max(maxAlpha, color.A);
            count++;
        }

        if (count == 0)
            return Color.Transparent;

        var alpha = count > 1 ? MathF.Min(0.4f, maxAlpha + 0.06f * (count - 1)) : maxAlpha;
        return new Color(r / count, g / count, b / count, alpha);
    }

    private static Color GetSingleColor(string id)
    {
        switch (id)
        {
            case "dropship_lz1":
                return Color.FromHex("#4FC3FF").WithAlpha(0.28f);
            case "dropship_lz2":
                return Color.FromHex("#7CFF7A").WithAlpha(0.28f);
            case "dropship_lz3":
                return Color.FromHex("#FFB74D").WithAlpha(0.28f);
            case "dropship_lz4":
                return Color.FromHex("#FF6B6B").WithAlpha(0.28f);
        }

        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in id)
            {
                hash ^= c;
                hash *= 16777619;
            }

            var red = (byte) (90 + (hash & 0x3F));
            var green = (byte) (120 + ((hash >> 6) & 0x3F));
            var blue = (byte) (160 + ((hash >> 12) & 0x3F));
            return new Color(red, green, blue, (byte) 71);
        }
    }
}
