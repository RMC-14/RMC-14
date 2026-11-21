using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._RMC14;

public static class CMStyleClasses
{
    public const string CMLabelAlignLeft = "CMAlignLeft";
    public const string RMCButtonDarkGold = "RMCButtonDarkGold";
    public const string RMCButtonBronzeGold = "RMCButtonBronzeGold";
    public const string RMCWindowDark = "RMCWindowDark";

    public static readonly Color DarkPanelBackground = Color.FromHex("#141414");

    private static readonly ButtonPalette DarkGoldPalette = new(
        Color.FromHex("#33302B"),
        Color.FromHex("#4A4234"),
        Color.FromHex("#6B5A3C"),
        Color.FromHex("#232323"),
        StyleNano.NanoGold,
        StyleNano.NanoGold,
        Color.FromHex("#E6D3AE"),
        StyleNano.DisabledFore);

    private static readonly ButtonPalette BronzeGoldPalette = new(
        Color.FromHex("#2B261E"),
        Color.FromHex("#3E3424"),
        Color.FromHex("#8A7048"),
        Color.FromHex("#1C1A17"),
        Color.FromHex("#C9A86A"),
        Color.FromHex("#E0C080"),
        Color.FromHex("#F5E6C8"),
        Color.FromHex("#5A5246"));

    public static IEnumerable<StyleRule> GetStyleRules()
    {
        foreach (var rule in GetButtonRules(RMCButtonDarkGold, DarkGoldPalette))
        {
            yield return rule;
        }

        foreach (var rule in GetButtonRules(RMCButtonBronzeGold, BronzeGoldPalette))
        {
            yield return rule;
        }

        yield return Child().Parent(Element<FancyWindow>().Class(RMCWindowDark))
            .Child(Element<PanelContainer>().Class(StyleBase.ClassAngleRect))
            .Prop(Control.StylePropertyModulateSelf, DarkPanelBackground);
    }

    private static IEnumerable<StyleRule> GetButtonRules(string styleClass, ButtonPalette palette)
    {
        (string Pseudo, Color Background, Color Text)[] states =
        [
            (ContainerButton.StylePseudoClassNormal, palette.Default, palette.Text),
            (ContainerButton.StylePseudoClassHover, palette.Hovered, palette.HoveredText),
            (ContainerButton.StylePseudoClassPressed, palette.Pressed, palette.PressedText),
            (ContainerButton.StylePseudoClassDisabled, palette.Disabled, palette.DisabledText),
        ];

        foreach (var (pseudo, background, text) in states)
        {
            yield return Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(styleClass)
                .Pseudo(pseudo)
                .Prop(Control.StylePropertyModulateSelf, background);

            yield return Child().Parent(Element<Button>().Class(styleClass).Pseudo(pseudo))
                .Child(Element<Label>())
                .Prop(Label.StylePropertyFontColor, text);
        }
    }

    private readonly record struct ButtonPalette(
        Color Default,
        Color Hovered,
        Color Pressed,
        Color Disabled,
        Color Text,
        Color HoveredText,
        Color PressedText,
        Color DisabledText);
}
