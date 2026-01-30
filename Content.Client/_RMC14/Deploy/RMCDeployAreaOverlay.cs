using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Deploy;

/// <summary>
/// Overlay for displaying the deployable area highlight in world space during deployment.
/// </summary>
public sealed class RMCDeployAreaOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public Box2 Box { get; set; }
    public Color Color { get; set; }

    public RMCDeployAreaOverlay() { }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Draw a filled area with transparency 0.5
        var fillColor = Color.WithAlpha(0.5f);
        args.WorldHandle.DrawRect(Box, fillColor);
    }
}
