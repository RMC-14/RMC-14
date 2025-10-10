using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.NightVision;

public sealed class HalfNightVisionBrightnessOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.DrawingHandle is not DrawingHandleWorld worldHandle)
            return;

        var worldBounds = args.WorldAABB;
        var brightnessColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);

        worldHandle.DrawRect(worldBounds, brightnessColor);
    }
}
