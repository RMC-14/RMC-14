using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EyeProtection;

/// <summary>
/// For eye protection (e.g. from welding)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCEyeProtectionSystem))]
public sealed partial class RMCEyeProtectionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    /// <summary>
    /// Which slots can the welding protection status be changed from?
    /// Currently supports only one slot at a time
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EYES;

    /// <summary>
    ///  Is welding protection enabled?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("toggled"), AutoNetworkedField]
    public bool Toggled = false;

    /// <summary>
    /// Equipped prefix for raised form
    /// </summary>
    [DataField]
    public string? RaisedEquippedPrefix;

    /// <summary>
    /// Name to display in pop-up messages (mainly for helmets and other equipment with integrated welding protection)
    /// </summary>
    [DataField]
    public string? PopupName;
}
