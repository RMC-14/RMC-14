using Content.Shared._RMC14.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity whenever it has a movement input change.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent
{
    public readonly Entity<InputMoverComponent> Entity;
    public readonly Entity<LinearInputMoverComponent> LinearMoverEntity; // Get smited by RMC!
    public readonly MoveButtons OldMovement;

    public bool HasDirectionalMovement => (Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;

    public MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement)
    {
        Entity = entity;
        OldMovement = oldMovement;
    }

    // RMC Smited!
    public MoveInputEvent(Entity<LinearInputMoverComponent> entity, MoveButtons oldMovement)
    {
        LinearMoverEntity = entity;
        OldMovement = oldMovement;
    }
}
