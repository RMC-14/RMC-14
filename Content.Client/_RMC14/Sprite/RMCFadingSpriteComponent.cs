namespace Content.Client._RMC14.Sprite;

/// <summary>
/// Client-side component for tracking the original alpha of a sprite when fading.
/// </summary>
[RegisterComponent, Access(typeof(RMCSpriteFadeSystem))]
public sealed partial class RMCFadingSpriteComponent : Component
{
    [ViewVariables]
    public float OriginalAlpha;

    /// <summary>
    /// Original alpha values for individual layers when fading specific layers
    /// </summary>
    [ViewVariables]
    public Dictionary<string, float> OriginalLayerAlphas = new();
}
