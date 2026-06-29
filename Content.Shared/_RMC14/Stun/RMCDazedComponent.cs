using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stun;

/// <summary>
///     Having this component prevents being dazed again.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCDazedSystem))]
public sealed partial class RMCDazedComponent : Component
{
    /// <summary>
    ///    How much extra wield delay to add when the mob is dazed.
    /// </summary>
    [DataField]
    public TimeSpan WieldDelayAdditional = TimeSpan.FromSeconds(0.5);
}
