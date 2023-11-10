using Content.Shared.Sprite;

namespace Content.Client._CM14.IconSmoothing;

[RegisterComponent]
public sealed partial class IconSmoothRandomComponent : Component
{
    /// <summary>
    ///     Which states to override with the ones from <see cref="RandomSpriteComponent"/>
    /// </summary>
    [DataField]
    public HashSet<string> Overrides = new();
}
