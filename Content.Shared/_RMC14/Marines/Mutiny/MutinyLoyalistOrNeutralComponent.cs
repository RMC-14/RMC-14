using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Mutiny;

/// <summary>
///     Restricts a marine to choosing between the loyalist and non-combatant sides when a mutiny begins.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutinyLoyalistOrNeutralComponent : Component;
