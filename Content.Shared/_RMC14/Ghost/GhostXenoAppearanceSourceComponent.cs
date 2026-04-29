using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent]
public sealed partial class GhostXenoAppearanceSourceComponent : Component
{
    [DataField(required: true)]
    public ResPath Sprite = default!;

    [DataField]
    public ResPath? OvipositorSprite;
}
