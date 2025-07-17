using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Deploy;

public sealed class RMCDeployAreaOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public Box2 Box { get; set; }
    public Color Color { get; set; }
    public bool Visible { get; set; }

    public RMCDeployAreaOverlay() { }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!Visible)
            return;

        // Draw a filled area with transparency 0.5
        var fillColor = Color.WithAlpha(0.5f);
        args.WorldHandle.DrawRect(Box, fillColor);
    }
}
