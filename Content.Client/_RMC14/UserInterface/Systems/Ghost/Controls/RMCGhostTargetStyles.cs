using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;

internal static class RMCGhostTargetStyles
{
    private const float RoundedPatchMargin = 5;
    private const float RoundedTextureScale = 0.4f;
    private const float InsetRoundedTextureScale = 0.2f;

    private static readonly ResPath RoundedButtonTexture =
        new("/Textures/Interface/Nano/rounded_button.svg.96dpi.png");

    public static StyleBoxTexture CreateRoundedBox(
        IResourceCache resourceCache,
        Color color,
        bool inset = false)
    {
        var textureScale = inset
            ? InsetRoundedTextureScale
            : RoundedTextureScale;
        var style = new StyleBoxTexture
        {
            Texture = resourceCache.GetTexture(RoundedButtonTexture),
            TextureScale = new Vector2(textureScale),
            Modulate = color,
        };
        style.SetPatchMargin(StyleBox.Margin.All, RoundedPatchMargin);
        return style;
    }
}
