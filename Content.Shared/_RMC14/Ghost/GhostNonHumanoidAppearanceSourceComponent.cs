using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent]
public sealed partial class GhostNonHumanoidAppearanceSourceComponent : Component
{
    [DataField(required: true)]
    public ResPath Sprite = default!;

    [DataField]
    public string? State;
}
