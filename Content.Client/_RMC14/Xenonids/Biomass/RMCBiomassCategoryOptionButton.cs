using Content.Client._RMC14.UserInterface;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Content.Client._RMC14.UserInterface.RMCMachineUiStyle;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassCategoryOptionButton : OptionButton
{
    public RMCBiomassCategoryOptionButton()
    {
        ModulateSelfOverride = Color.White;
        StyleBoxOverride = new RMCBevelStyleBox
        {
            BackgroundColor = ButtonColor,
            ContentMarginLeftOverride = 8,
            ContentMarginRightOverride = 8,
            ContentMarginTopOverride = 3,
            ContentMarginBottomOverride = 3,
        };
        RecolorChildren(this);
    }

    private static void RecolorChildren(Control control)
    {
        foreach (var child in control.Children)
        {
            switch (child)
            {
                case Label label:
                    label.FontColorOverride = ButtonTextColor;
                    break;
                case TextureRect triangle:
                    triangle.Modulate = ButtonTextColor;
                    break;
                default:
                    RecolorChildren(child);
                    break;
            }
        }
    }

    public override void ButtonOverride(Button button)
    {
        base.ButtonOverride(button);

        button.Modulate = Color.White;
        button.ModulateSelfOverride = Color.White;
        button.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = WindowColor,
            BorderColor = Color.Black,
            BorderThickness = new Thickness(1),
        };
        button.Label.FontColorOverride = Color.White;
    }
}
