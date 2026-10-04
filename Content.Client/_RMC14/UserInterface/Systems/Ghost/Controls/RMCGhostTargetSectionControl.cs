using Content.Client.Stylesheets;
using Content.Shared._RMC14.Ghost;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;

internal sealed class RMCGhostTargetSectionControl
{
    private const float HoverLightnessPercent = 0.12f;

    private readonly StyleBoxTexture _headingStyle;
    private Color _headerColor;

    public readonly CollapsibleHeading Heading;
    public readonly CollapsibleBody Body;
    public readonly RMCGhostTargetWrapContainer Targets;
    public readonly BoxContainer Children;
    public readonly Collapsible Collapsible;

    public RMCGhostTargetSectionControl(IResourceCache resourceCache, bool isSubsection)
    {
        Heading = new CollapsibleHeading();
        Heading.Label.StyleClasses.Add(StyleNano.StyleClassLabelSmall);
        Heading.Label.FontOverride = resourceCache.NotoStack("Bold", 11);
        Heading.ChevronMargin = new Thickness(4, 0, 3, 0);
        _headingStyle = RMCGhostTargetStyles.CreateRoundedBox(resourceCache, Color.White);
        _headingStyle.SetContentMarginOverride(StyleBox.Margin.Top, 3);
        _headingStyle.SetContentMarginOverride(StyleBox.Margin.Left, 3);
        _headingStyle.SetContentMarginOverride(StyleBox.Margin.Right, 6);
        _headingStyle.SetContentMarginOverride(StyleBox.Margin.Bottom, 3);
        Heading.StyleBoxOverride = _headingStyle;
        Heading.ModulateSelfOverride = Color.White;
        Heading.OnMouseEntered += _ => SetHeadingColor(AdjustLightness(_headerColor, HoverLightnessPercent));
        Heading.OnMouseExited += _ => SetHeadingColor(_headerColor);

        Body = new CollapsibleBody
        {
            HorizontalExpand = true,
            Margin = new Thickness(2, 4, 0, 0),
        };
        Targets = new RMCGhostTargetWrapContainer
        {
            HorizontalExpand = true,
            VerticalExpand = false,
        };

        Children = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = false,
        };
        var main = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = false,
        };
        main.AddChild(Targets);
        main.AddChild(Children);
        Body.AddChild(main);

        Collapsible = new Collapsible(Heading, Body)
        {
            Margin = isSubsection ? new Thickness(12, 3, 0, 3) : new Thickness(0, 3),
            HorizontalExpand = true,
        };
    }

    public void Update(RMCGhostTargetSection section, string title, int totalCount)
    {
        _headerColor = section.HeaderColor;
        Heading.Title = $"{title} - ({totalCount})";
        SetHeadingColor(_headerColor);
        Targets.Visible = section.Targets.Count > 0;
        Children.Visible = section.Children.Count > 0;
    }

    private void SetHeadingColor(Color color)
    {
        _headingStyle.Modulate = color;
    }

    private static Color AdjustLightness(Color color, float percent)
    {
        var hsv = Color.ToHsv(color);
        hsv.Z = percent > 0
            ? Math.Min(hsv.Z * (1f + percent), 1f)
            : hsv.Z * (1f + percent);
        return Color.FromHsv(hsv);
    }
}
