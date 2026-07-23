using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class GhostNonHumanoidAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public ResPath? Sprite;

    [DataField, AutoNetworkedField]
    public string? State;

    [DataField, AutoNetworkedField]
    public string? SourcePrototype;

}
