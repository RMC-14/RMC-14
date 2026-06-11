using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMovementSystem))]
public sealed partial class RMCOutsideChamberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Chamber;
}
