using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

/// <summary>
/// Component that allows fires to bypass fire immunity.
/// This should be added to fire entities (like certain tile fires) to make them
/// able to damage entities that normally have fire immunity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCFireImmunityBypassComponent : Component;
