using Content.Client.Stylesheets;
using Content.Shared._RMC14.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;

internal sealed class RMCGhostTargetSectionControl
{
    private const float HoverLightnessPercent = 0.1f;
    private Color _headerColor;

    public readonly CollapsibleHeading Heading;
    public readonly CollapsibleBody Body;
    public readonly GridContainer Targets;
    public readonly BoxContainer Children;
    public readonly Collapsible Collapsible;

    public RMCGhostTargetSectionControl(
        SpriteSystem spriteSystem,
        IResourceCache resourceCache,
        bool isSubsection)
    {
        Heading = new CollapsibleHeading();
        Heading.Label.StyleClasses.Add(StyleNano.StyleClassLabelSmall);
        Heading.Label.FontOverride = resourceCache.NotoStack("Bold", 10);
        Heading.StyleBoxOverride = new StyleBoxTexture
        {
            Texture = spriteSystem.Frame0(
                new SpriteSpecifier.Texture(
                    new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png"))),
            PatchMarginTop = 5,
            PatchMarginBottom = 5,
            PatchMarginLeft = 5,
            PatchMarginRight = 5,
            ContentMarginTopOverride = 3,
            ContentMarginLeftOverride = 5,
            ContentMarginRightOverride = 5,
            ContentMarginBottomOverride = 3,
            Padding = new Thickness(2),
        };
        Heading.OnMouseEntered += _ => SetHeadingColor(AdjustLightness(_headerColor, HoverLightnessPercent));
        Heading.OnMouseExited += _ => SetHeadingColor(_headerColor);

        Body = new CollapsibleBody
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 3, 0, 0),
        };
        Targets = new GridContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        Body.OnResized += () =>
        {
            if (Body.Width > 0)
                Targets.MaxGridWidth = Body.Width;
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
            Margin = isSubsection ? new Thickness(10, 4, 0, 4) : new Thickness(0, 4),
            HorizontalExpand = true,
        };
    }

    public void Update(RMCGhostTargetSection section, string title, int totalCount)
    {
        _headerColor = section.HeaderColor;
        Heading.Title = $"{title} - ({totalCount})";
        SetHeadingColor(_headerColor);
        Collapsible.BodyVisible = section.IsExpandedByDefault;
        Targets.Visible = section.Targets.Count > 0;
        Children.Visible = section.Children.Count > 0;
    }

    private void SetHeadingColor(Color color)
    {
        if (Heading.StyleBoxOverride is StyleBoxTexture style)
            style.Modulate = color;
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
