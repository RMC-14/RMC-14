using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCInteractionSystem))]
public sealed partial class MaxRotationComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle Set;

    [DataField, AutoNetworkedField]
    public Angle Deviation;
}
