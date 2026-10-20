using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public static class RMCMachineUiStyle
{
    public static readonly Color HeaderColor = Color.FromHex("#12141A");
    public static readonly Color GreenColor = Color.FromHex("#4CAF50");
    public static readonly Color TanColor = Color.FromHex("#ffb950");
    public static readonly Color OrangeColor = Color.FromHex("#C99A29");
    public static readonly Color BlueColor = Color.FromHex("#3A7CC4");
    public static readonly Color RedColor = Color.FromHex("#8B2020");
    public static readonly Color GreyButtonFill = Color.FromHex("#bfbfbf");
    public static readonly Color DarkRowColor = Color.FromHex("#22262F");

    public static StyleBoxFlat Flat(Color color)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 1,
            ContentMarginBottomOverride = 1,
        };
    }

    public static void Header(PanelContainer panel)
    {
        panel.PanelOverride = Flat(HeaderColor);
    }

    public static void FilledButton(Button button, Color color)
    {
        button.StyleBoxOverride = Flat(color);
        button.ModulateSelfOverride = Color.White;
    }

    public static void GreyButton(Button button)
    {
        FilledButton(button, GreyButtonFill);
    }

    public static void StatusRow(Control panelControl, Label label, string text, Color color, Font? font = null)
    {
        label.Text = text;
        label.FontColorOverride = color == RedColor ? Color.White : Color.Black;
        if (font != null)
            label.FontOverride = font;

        if (panelControl is PanelContainer panel)
            panel.PanelOverride = Flat(color);
    }
}
