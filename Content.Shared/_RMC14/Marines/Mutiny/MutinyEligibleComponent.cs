using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Mutiny;

/// <summary>
///     Marks a character as ineligible for mutiny recruitment.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutinyEligibleComponent : Component;

