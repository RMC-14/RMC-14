using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Mutiny;

/// <summary>
///     Makes a marine automatically join the loyalist side when a mutiny begins.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutinyForcedLoyalistComponent : Component;
