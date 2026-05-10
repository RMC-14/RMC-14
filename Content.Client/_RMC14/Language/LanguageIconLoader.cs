using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Language;

public static class LanguageIconLoader
{
    public static Texture? Load(IResourceCache resourceCache, SpriteSystem spriteSystem, string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
            return null;

        if (TryParseRsiState(icon, out var rsiPath, out var state))
            return spriteSystem.Frame0(new SpriteSpecifier.Rsi(rsiPath, state));

        return resourceCache.TryGetResource<TextureResource>(icon, out var texture)
            ? texture.Texture
            : null;
    }

    private static bool TryParseRsiState(string icon, out ResPath rsiPath, out string state)
    {
        const string marker = ".rsi/";

        var markerIndex = icon.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex == -1)
        {
            rsiPath = default;
            state = string.Empty;
            return false;
        }

        var stateStart = markerIndex + marker.Length;
        if (stateStart >= icon.Length)
        {
            rsiPath = default;
            state = string.Empty;
            return false;
        }

        state = icon[stateStart..];
        if (state.Contains('.'))
        {
            rsiPath = default;
            state = string.Empty;
            return false;
        }

        rsiPath = new ResPath(icon[..(markerIndex + ".rsi".Length)]);
        return true;
    }
}
