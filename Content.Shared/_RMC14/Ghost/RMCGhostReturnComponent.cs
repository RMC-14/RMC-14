using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCGhostReturnComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}
