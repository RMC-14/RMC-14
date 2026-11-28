using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._RMC14.UserInterface.Machine;

public sealed class RMCMachineScrollContainer : ScrollContainer
{
    private static readonly Stylesheet ScrollBarStylesheet = new(new[]
    {
        ScrollBarRule<VScrollBar>(null, RMCMachineUiStyle.ScrollThumbColor),
        ScrollBarRule<VScrollBar>(ScrollBar.StylePseudoClassHover, RMCMachineUiStyle.ScrollThumbHoverColor),
        ScrollBarRule<VScrollBar>(ScrollBar.StylePseudoClassGrabbed, RMCMachineUiStyle.ScrollThumbGrabbedColor),
        ScrollBarRule<HScrollBar>(null, RMCMachineUiStyle.ScrollThumbColor),
        ScrollBarRule<HScrollBar>(ScrollBar.StylePseudoClassHover, RMCMachineUiStyle.ScrollThumbHoverColor),
        ScrollBarRule<HScrollBar>(ScrollBar.StylePseudoClassGrabbed, RMCMachineUiStyle.ScrollThumbGrabbedColor),
    });

    public RMCMachineScrollContainer()
    {
        foreach (var child in Children)
        {
            if (child is ScrollBar bar)
                bar.Stylesheet = ScrollBarStylesheet;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var child in Children)
        {
            if (child is ScrollBar { Visible: true } bar)
                handle.DrawRect(UIBox2.FromDimensions(bar.PixelPosition, bar.PixelSize), RMCMachineUiStyle.ScrollTrackColor);
        }
    }

    private static StyleRule ScrollBarRule<T>(string? pseudoClass, Color color) where T : ScrollBar
    {
        var selector = Element<T>();
        if (pseudoClass != null)
            selector = selector.Pseudo(pseudoClass);

        return selector.Prop(ScrollBar.StylePropertyGrabber,
            new StyleBoxFlat(color)
            {
                ContentMarginLeftOverride = typeof(T) == typeof(VScrollBar) ? StyleBase.DefaultGrabberSize : null,
                ContentMarginTopOverride = StyleBase.DefaultGrabberSize,
            });
    }
}
