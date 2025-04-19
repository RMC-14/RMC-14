using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
///     Having this component prevents being dazed again.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCDazedSystem))]
public sealed partial class RMCDazedComponent : Component;
