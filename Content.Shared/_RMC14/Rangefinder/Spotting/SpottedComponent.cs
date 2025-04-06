using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpottedComponent : Component
{
    /// <summary>
    ///     The duration multiplier for any aimed shots done at an entity with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AimDurationMultiplier = 0.5f;
}
