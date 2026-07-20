using Robust.Client.Graphics;

namespace Content.Client._RMC14.UserInterface.Crt;

[Flags]
public enum RMCCrtEffects
{
    None = 0,
    HorizontalScanlines = 1 << 0,
    RgbSubpixels = 1 << 1,
    DiagonalStripes = 1 << 2,
}

public enum RMCCrtPalettePreset
{
    Blue,
    Brown,
    Custom,
    Green,
    Purple,
    Red,
    Spp,
    White,
    Yellow,
}

public enum RMCCrtPanelVariant
{
    Inset,
    Surface,
    Transparent,
    Warning,
}

public enum RMCCrtButtonVariant
{
    Danger,
    Filled,
    Navigation,
    Outline,
}

public enum RMCCrtContentAlignment
{
    Center,
    Left,
    Right,
}

public enum RMCCrtTone
{
    Default,
    Danger,
    Good,
    Muted,
    Warning,
}

public enum RMCCrtSeparatorOrientation
{
    Horizontal,
    Vertical,
}

public readonly record struct RMCCrtPalette(
    Color Foreground,
    Color Background,
    Color Border,
    Color Fill,
    Color FillForeground,
    Color Good,
    Color Warning,
    Color Danger,
    Color Muted,
    Color DisabledBackground,
    Color DisabledForeground)
{
    public Color HoverBackground => Color.InterpolateBetween(Background, Fill, 0.3f);

    public Color PressedBackground => Color.InterpolateBetween(Background, Fill, 0.65f);
}

public static class RMCCrtPalettes
{
    private static readonly Color Good = Color.FromHex("#00C957");
    private static readonly Color Warning = Color.FromHex("#D3B400");
    private static readonly Color Danger = Color.FromHex("#F04B43");

    public static RMCCrtPalette Get(RMCCrtPalettePreset preset)
    {
        return preset switch
        {
            RMCCrtPalettePreset.Blue => Create("#8ACBFF", "#00000F", "#82C5F2"),
            RMCCrtPalettePreset.Brown => Create("#AC8710", "#0F0F00", "#AC8710"),
            RMCCrtPalettePreset.Green => Create("#00EB4E", "#001000", "#00EB4E"),
            RMCCrtPalettePreset.Purple => Create("#C634D0", "#100302", "#C634D0"),
            RMCCrtPalettePreset.Red => Create("#D03434", "#100302", "#D03434"),
            RMCCrtPalettePreset.Spp => Create("#DBBF23", "#511814", "#DBBF23"),
            RMCCrtPalettePreset.White => Create("#CCCCCC", "#666666", "#CCCCCC"),
            RMCCrtPalettePreset.Yellow => Create("#FFD000", "#101000", "#FFD000"),
            _ => Create("#8ACBFF", "#00000F", "#82C5F2"),
        };
    }

    private static RMCCrtPalette Create(string foreground, string background, string fill)
    {
        var foregroundColor = Color.FromHex(foreground);
        return new RMCCrtPalette(
            foregroundColor,
            Color.FromHex(background),
            foregroundColor,
            Color.FromHex(fill),
            Color.FromHex(background),
            Good,
            Warning,
            Danger,
            Color.InterpolateBetween(Color.FromHex(background), foregroundColor, 0.48f),
            Color.FromHex(background).WithAlpha(0.5f),
            Color.InterpolateBetween(Color.FromHex(background), foregroundColor, 0.42f));
    }
}

public static class RMCCrtIcons
{
    public const string Ban = "ban";
    public const string Bullhorn = "bullhorn";
    public const string Cog = "cog";
    public const string DoorOpen = "door_open";
    public const string Heartbeat = "heartbeat";
    public const string Home = "home";
    public const string IdCard = "id_card";
    public const string Map = "map";
    public const string Medal = "medal";
    public const string PaperPlane = "paper_plane";
    public const string Users = "users";
    public const string Warning = "warning";
}

public static class RMCCrtStyleClasses
{
    public const string Heading = "RMCCrtHeading";
    public const string MutedIcon = "RMCCrtMutedIcon";
    public const string Text = "RMCCrtText";
}
