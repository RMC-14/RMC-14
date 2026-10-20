using Content.Shared._RMC14.Botany;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Botany;

internal static class BotanyUiStyle
{
    public static readonly Color StatusStableColor = Color.FromHex("#2C6E55");
    public static readonly Color StatusSeriousColor = Color.FromHex("#8B6334");
    public static readonly Color StatusCriticalColor = Color.FromHex("#8B2F35");
    public static readonly Color StatusInfoColor = Color.FromHex("#2F5D8A");
    public static readonly Color StatusEmptyColor = Color.FromHex("#4A5058");

    public static readonly Color ValueGoodColor = Color.FromHex("#25B732");
    public static readonly Color ValueWarnColor = Color.FromHex("#FFAA00");
    public static readonly Color ValueBadColor = Color.FromHex("#FF6060");

    public static string ShortSeedName(string name)
    {
        const string prefix = "packet of ";
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? name[prefix.Length..]
            : name;
    }

    public static string GetHarvestLabel(HarvestType type)
    {
        return type switch
        {
            HarvestType.Repeat => "Repeating",
            HarvestType.SelfHarvest => "Self-harvesting",
            _ => "No repeat",
        };
    }
}
