using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Content.Client._RMC14.Xenonids.Biomass.RMCBiomassAnalyzerColors;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassCategoryOptionButton : OptionButton
{
    public RMCBiomassCategoryOptionButton()
    {
        RecolorChildren(this);
    }

    private static void RecolorChildren(Control control)
    {
        foreach (var child in control.Children)
        {
            switch (child)
            {
                case Label label:
                    label.FontColorOverride = AmberBlack;
                    break;
                case TextureRect triangle:
                    triangle.Modulate = AmberBlack;
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
            BackgroundColor = AmberBlack,
            BorderColor = AmberDim,
            BorderThickness = new Thickness(2),
        };
        button.Label.FontColorOverride = Amber;
    }
}
