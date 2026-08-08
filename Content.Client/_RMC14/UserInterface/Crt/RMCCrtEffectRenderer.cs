using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.UserInterface.Crt;

internal sealed class RMCCrtEffectRenderer
{
    private static readonly ProtoId<ShaderPrototype> Shader = "RMCCrtUiEffects";

    private readonly ShaderInstance _shader;

    public RMCCrtEffectRenderer()
    {
        _shader = IoCManager.Resolve<IPrototypeManager>().Index(Shader).InstanceUnique();
    }

    public void Draw(
        DrawingHandleScreen handle,
        float width,
        float height,
        float uiScale,
        RMCCrtEffects effects,
        float scanlineSpacing,
        float scanlineThickness,
        float rgbWidth,
        float stripeWidth,
        float scanlineOpacity,
        float rgbOpacity,
        Color stripeColor)
    {
        if (effects == RMCCrtEffects.None || width <= 0 || height <= 0)
            return;

        _shader.SetParameter("size", new Vector2(width, height));
        _shader.SetParameter(
            "horizontalScanlines",
            (effects & RMCCrtEffects.HorizontalScanlines) != 0);
        _shader.SetParameter(
            "rgbSubpixels",
            (effects & RMCCrtEffects.RgbSubpixels) != 0);
        _shader.SetParameter(
            "diagonalStripes",
            (effects & RMCCrtEffects.DiagonalStripes) != 0);
        _shader.SetParameter("scanlineSpacing", Math.Max(2f, scanlineSpacing * uiScale));
        _shader.SetParameter("scanlineThickness", Math.Max(1f, scanlineThickness * uiScale));
        _shader.SetParameter("rgbWidth", Math.Max(1f, rgbWidth * uiScale));
        _shader.SetParameter("stripeWidth", Math.Max(2f, stripeWidth * uiScale));
        _shader.SetParameter("scanlineOpacity", Math.Clamp(scanlineOpacity, 0, 1));
        _shader.SetParameter("rgbOpacity", Math.Clamp(rgbOpacity, 0, 1));
        _shader.SetParameter("stripeColor", stripeColor);

        var previousShader = handle.GetShader();
        handle.UseShader(_shader);
        handle.DrawRect(UIBox2.FromDimensions(Vector2.Zero, new Vector2(width, height)), Color.White);
        handle.UseShader(previousShader);
    }
}
