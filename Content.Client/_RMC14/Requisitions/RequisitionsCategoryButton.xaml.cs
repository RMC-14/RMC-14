using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsCategoryButton : Button
{
    public RequisitionsCategoryButton()
    {
        RobustXamlLoader.Load(this);
    }

    public void ApplyBlackMarketStyle()
    {
        StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#311218"),
            BorderColor = Color.FromHex("#ce3349"),
            BorderThickness = new Thickness(2),
        };
    }
}
