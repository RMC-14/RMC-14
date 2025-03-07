using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpottedComponent : Component
{
    /// <summary>
    ///     The duration multiplier for any aimed shots done at an entity with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double AimDurationMultiplier = 0.5;

    /// <summary>
    ///     The entity spotting this target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Spotter;
}
