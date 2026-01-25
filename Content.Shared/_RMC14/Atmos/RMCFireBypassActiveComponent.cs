using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

/// <summary>
/// Component that tracks whether the entity's current fire state should bypass fire immunity.
/// This is added to entities when they are ignited by a fire source that has bypass capabilities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCFireBypassActiveComponent : Component;
