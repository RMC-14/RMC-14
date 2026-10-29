using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Botany;

public sealed class RMCBotanyButton : Button
{
    private static readonly Color NormalBackground = Color.FromHex("#263445");
    private static readonly Color NormalBorder = Color.FromHex("#4972A1");
    private static readonly Color NormalText = Color.FromHex("#E6EDF3");

    private static readonly Color HoverBackground = Color.FromHex("#30455C");
    private static readonly Color HoverBorder = Color.FromHex("#6A93C4");

    private static readonly Color PressedBackground = Color.FromHex("#4972A1");

    private static readonly Color DisabledBackground = Color.FromHex("#1B1E22");
    private static readonly Color DisabledBorder = Color.FromHex("#2E343A");
    private static readonly Color DisabledText = Color.FromHex("#5A636C");

    private readonly StyleBoxFlat _box = new()
    {
        BorderThickness = new Thickness(1),
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8,
        ContentMarginTopOverride = 2,
        ContentMarginBottomOverride = 2,
    };

    public RMCBotanyButton()
    {
        StyleBoxOverride = _box;
        ModulateSelfOverride = Color.White;
        UpdateStyle();
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        var (background, border, text) = DrawMode switch
        {
            DrawModeEnum.Disabled => (DisabledBackground, DisabledBorder, DisabledText),
            DrawModeEnum.Pressed => (PressedBackground, HoverBorder, Color.White),
            DrawModeEnum.Hover => (HoverBackground, HoverBorder, Color.White),
            _ => (NormalBackground, NormalBorder, NormalText),
        };

        _box.BackgroundColor = background;
        _box.BorderColor = border;

        if (Label != null)
            Label.FontColorOverride = text;
    }
}
