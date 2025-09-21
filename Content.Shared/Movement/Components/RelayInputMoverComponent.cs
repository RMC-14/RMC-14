using Content.Shared._RMC14.Movement;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Raises the engine movement inputs for a particular entity onto the designated entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMoverController), typeof(SharedLinearMoverController))] // RMC change
public sealed partial class RelayInputMoverComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid RelayEntity;
}
