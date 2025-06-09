using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCConstructionPreventCollideComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 0.75f;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}
