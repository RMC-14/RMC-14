namespace Content.Shared._RMC14.Stun;

/// <summary>
///     Having this component prevents being dazed again.
/// </summary>
[RegisterComponent, Access(typeof(RMCDazedSystem))]
public sealed partial class RMCDazedComponent : Component
{
}
