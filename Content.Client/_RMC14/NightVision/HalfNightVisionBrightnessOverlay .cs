using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.NightVision;

public sealed class HalfNightVisionBrightnessOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    private readonly float _brightness;

    public HalfNightVisionBrightnessOverlay(float brightness = 0.45f)
    {
        _brightness = brightness;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.DrawingHandle is not DrawingHandleWorld worldHandle)
            return;

        var worldBounds = args.WorldAABB;
        var brightnessColor = new Color(_brightness, _brightness, _brightness, 1.0f);

        worldHandle.DrawRect(worldBounds, brightnessColor);
    }
}
