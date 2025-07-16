using Robust.Client.Graphics;
using Robust.Shared.Maths;
using System.Numerics;
using Robust.Shared.Enums;

namespace Content.Client._RMC14.Deploy;

public sealed class DeployAreaOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public Vector2 Center { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Color Color { get; set; }
    public bool Visible { get; set; }

    public DeployAreaOverlay() { }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!Visible)
            return;

        var box = new Box2(
            Center - new Vector2(Width, Height) / 2,
            Center + new Vector2(Width, Height) / 2
        );

        // Рисуем залитую зону с прозрачностью 0.6
        var fillColor = Color.WithAlpha(0.6f);
        args.WorldHandle.DrawRect(box, fillColor);
    }
}
