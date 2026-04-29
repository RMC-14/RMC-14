using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostXenoAppearanceComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ResPath Sprite = default!;

    [DataField, AutoNetworkedField]
    public ResPath? OvipositorSprite;

    [DataField, AutoNetworkedField]
    public string? OvipositorState;

    [DataField, AutoNetworkedField]
    public bool SpentParasite;
}
