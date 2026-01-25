using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Deafness;

/// <summary>
///     Having this component on a clothing item or the mob itself will make it immune to taking ear damage (deafness)
/// </summary>
[Access(typeof(SharedDeafnessSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCEarProtectionComponent : Component;
