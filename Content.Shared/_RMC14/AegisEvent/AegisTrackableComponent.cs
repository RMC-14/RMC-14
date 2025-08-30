using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AegisEvent;

/// <summary>
/// Component that marks an entity as trackable by AEGIS pinpointers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AegisTrackableComponent : Component
{
}
