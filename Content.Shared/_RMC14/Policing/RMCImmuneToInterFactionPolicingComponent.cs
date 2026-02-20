using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Policing;

/// <summary>
///     Mobs with this component become more resistant to the effects of policing gear from other factions.
///     Policing gear from their own faction(s) will still be as effective.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCImmuneToInterFactionPolicingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RemoveOnCuffed = true;

    /// <summary>
    ///     For example, a multiplier of 0.1 would reduce stuns on policing equipment by 90%
    ///     or would reduce the stamina damage of equipment by 90%.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EffectivenessMultiplier = 0.1f;
}
