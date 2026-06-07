using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Restricts item pickup, equip, and use by synth/non-synth state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSynthItemRestrictionSystem))]
public sealed partial class RMCSynthItemRestrictionComponent : Component
{
    /// <summary>
    /// If true, only synths are allowed. If false, synths are blocked instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SynthOnly = true;

    /// <summary>
    /// Whether pickup attempts should be checked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckPickup = true;

    /// <summary>
    /// Whether equip attempts should be checked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckEquip = true;

    /// <summary>
    /// Whether activation and interaction attempts should be checked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CheckUse = true;

    /// <summary>
    /// Popup shown when the restriction blocks the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId DenyPopup = "rmc-synth-item-restricted";
}
