using Content.Shared._RMC14.Movement;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMoverController), typeof(SharedLinearMoverController))] // RMC change
public sealed partial class MovementRelayTargetComponent : Component
{
    /// <summary>
    /// The entity that is relaying to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Source;
}
