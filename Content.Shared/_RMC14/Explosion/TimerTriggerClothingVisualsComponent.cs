using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

/// <summary>
/// Updates clothing equipped visuals when timer trigger is activated (primed).
/// Uses prefix to switch between equipped states.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TimerTriggerClothingVisualsComponent : Component
{
    /// <summary>
    /// Prefix to apply to equipped clothing sprite when primed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PrimedPrefix = "up";
}
