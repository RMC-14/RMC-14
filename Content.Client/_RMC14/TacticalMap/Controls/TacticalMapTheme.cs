using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap.Controls;

public interface ITacticalMapThemed
{
    void ApplyTheme(TacticalMapTheme theme);
}

public enum TacticalMapTextStyle
{
    Normal,
    Muted,
    Small,
    Heading,
    Title,
}

public sealed record TacticalMapTheme(
    Color Background,
    Color Surface,
    Color Raised,
    Color Border,
    Color Divider,
    Color Text,
    Color TextMuted,
    Color TextDisabled,
    Color TextInverse,
    Color Hover,
    Color Selected,
    Color Primary,
    Color PrimaryText,
    Color Danger,
    Color Good,
    Color Warning,
    string FontPath,
    string BoldFontPath)
{
    public static readonly TacticalMapTheme Marine = new(
        Background: Color.FromHex("#050A14"),
        Surface: Color.FromHex("#0A1322"),
        Raised: Color.FromHex("#101D33"),
        Border: Color.FromHex("#2A4766"),
        Divider: Color.FromHex("#8AC8FF").WithAlpha(0.18f),
        Text: Color.FromHex("#8AC8FF"),
        TextMuted: Color.FromHex("#5D86AB"),
        TextDisabled: Color.FromHex("#34506B"),
        TextInverse: Color.FromHex("#00000F"),
        Hover: Color.FromHex("#8AC8FF").WithAlpha(0.16f),
        Selected: Color.FromHex("#8AC8FF"),
        Primary: Color.FromHex("#2F6FA8"),
        PrimaryText: Color.FromHex("#E6F3FF"),
        Danger: Color.FromHex("#D9534F"),
        Good: Color.FromHex("#67D692"),
        Warning: Color.FromHex("#E0A84A"),
        FontPath: "/Fonts/RobotoMono/RobotoMono-Regular.ttf",
        BoldFontPath: "/Fonts/RobotoMono/RobotoMono-Bold.ttf");

    public static readonly TacticalMapTheme Xeno = new(
        Background: Color.FromHex("#121318"),
        Surface: Color.FromHex("#1B1E25"),
        Raised: Color.FromHex("#252833"),
        Border: Color.FromHex("#52375C"),
        Divider: Color.FromHex("#D6C2E6").WithAlpha(0.15f),
        Text: Color.FromHex("#D6C2E6"),
        TextMuted: Color.FromHex("#9580A8"),
        TextDisabled: Color.FromHex("#574a63"),
        TextInverse: Color.FromHex("#F4ECFA"),
        Hover: Color.FromHex("#52375C").WithAlpha(0.55f),
        Selected: Color.FromHex("#7A4F8C"),
        Primary: Color.FromHex("#52375C"),
        PrimaryText: Color.FromHex("#F4ECFA"),
        Danger: Color.FromHex("#C9433F"),
        Good: Color.FromHex("#7BC96F"),
        Warning: Color.FromHex("#D9A441"),
        FontPath: "/Fonts/NotoSans/NotoSans-Regular.ttf",
        BoldFontPath: "/Fonts/NotoSans/NotoSans-Bold.ttf");
}

public static class TacticalMapStyle
{
    public const int FontSize = 10;
    public const int SmallFontSize = 9;
    public const int HeadingFontSize = 9;
    public const int TitleFontSize = 11;

    private static readonly Dictionary<(string Path, int Size), Font> Fonts = new();

    public static Font Font(TacticalMapTheme theme, TacticalMapTextStyle style = TacticalMapTextStyle.Normal)
    {
        var (path, size) = style switch
        {
            TacticalMapTextStyle.Title => (theme.BoldFontPath, TitleFontSize),
            TacticalMapTextStyle.Heading => (theme.BoldFontPath, HeadingFontSize),
            TacticalMapTextStyle.Small => (theme.FontPath, SmallFontSize),
            _ => (theme.FontPath, FontSize),
        };

        if (Fonts.TryGetValue((path, size), out var font))
            return font;

        font = IoCManager.Resolve<IResourceCache>().GetFont(path, size);
        Fonts[(path, size)] = font;
        return font;
    }

    public static Color TextColor(TacticalMapTheme theme, TacticalMapTextStyle style)
    {
        return style switch
        {
            TacticalMapTextStyle.Muted or TacticalMapTextStyle.Heading => theme.TextMuted,
            _ => theme.Text,
        };
    }

    public static void StyleLabel(Label label, TacticalMapTheme theme, TacticalMapTextStyle style = TacticalMapTextStyle.Normal)
    {
        label.FontOverride = Font(theme, style);
        label.FontColorOverride = TextColor(theme, style);
    }

    public static StyleBoxFlat Box(Color background, Color? border = null, float borderThickness = 1, float padding = 0)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = background,
            BorderColor = border ?? Color.Transparent,
            BorderThickness = new Thickness(border == null ? 0 : borderThickness),
        };

        if (padding > 0)
            box.SetContentMarginOverride(StyleBox.Margin.All, padding);

        return box;
    }

    public static StyleBoxFlat Rule(TacticalMapTheme theme, bool bottom)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = theme.Raised,
            BorderColor = theme.Border,
            BorderThickness = bottom ? new Thickness(0, 0, 0, 1) : new Thickness(0, 1, 0, 0),
        };
    }

    public static void StyleSlider(Slider slider, TacticalMapTheme theme)
    {
        slider.BackgroundStyleBoxOverride = Box(theme.Surface, theme.Border);
        slider.ForegroundStyleBoxOverride = Box(Color.Transparent);
        slider.FillStyleBoxOverride = Box(theme.Text.WithAlpha(0.35f));
        var grabber = Box(theme.Text, theme.Background);
        grabber.SetContentMarginOverride(StyleBox.Margin.All, 5);
        slider.GrabberStyleBoxOverride = grabber;
    }

    public static void StyleProgressBar(ProgressBar bar, TacticalMapTheme theme, Color fill)
    {
        bar.BackgroundStyleBoxOverride = Box(theme.Surface, theme.Border);
        bar.ForegroundStyleBoxOverride = Box(fill.WithAlpha(0.55f));
    }

    public static void ApplyTheme(Control root, TacticalMapTheme theme)
    {
        if (root is ITacticalMapThemed themed)
            themed.ApplyTheme(theme);

        foreach (var child in root.Children)
        {
            ApplyTheme(child, theme);
        }
    }
}
