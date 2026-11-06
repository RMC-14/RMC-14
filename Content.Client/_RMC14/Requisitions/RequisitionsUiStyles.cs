using System.Linq;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._RMC14.Requisitions;

public static class RequisitionsUiStyles
{
    public const string ShopTabs = "RMCRequisitionsShopTabs";

    public static Stylesheet Create()
    {
        var baseRules = IoCManager.Resolve<IStylesheetManager>().SheetNano.Rules;
        return new Stylesheet(baseRules.Concat(CreateRules()).ToList());
    }

    public static void ApplyQuantityButton(Button button)
    {
        button.StyleBoxOverride = Box("#30313f", "#9ccfff", new Thickness(2));
        button.MinWidth = Math.Max(button.MinWidth, 36);
        button.MinHeight = Math.Max(button.MinHeight, 30);
    }

    private static StyleRule[] CreateRules()
    {
        var activeTab = Box("#3f4050", "#9ccfff", new Thickness(2));
        activeTab.SetContentMarginOverride(StyleBox.Margin.Horizontal, 9);
        activeTab.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);

        var inactiveTab = Box("#20212a", "#5e6f91", new Thickness(2));
        inactiveTab.SetContentMarginOverride(StyleBox.Margin.Horizontal, 9);
        inactiveTab.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);

        var panel = Box("#2f303e", "#405f8e", new Thickness(2));

        return
        [
            Element<TabContainer>()
                .Class(ShopTabs)
                .Prop(TabContainer.StylePropertyPanelStyleBox, panel)
                .Prop(TabContainer.StylePropertyTabStyleBox, activeTab)
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, inactiveTab)
                .Prop(TabContainer.stylePropertyTabFontColor, Color.White)
                .Prop(TabContainer.StylePropertyTabFontColorInactive, Color.FromHex("#b8bcc9")),
        ];
    }

    private static StyleBoxFlat Box(string background, string border, Thickness thickness)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex(background),
            BorderColor = Color.FromHex(border),
            BorderThickness = thickness,
        };
    }
}
