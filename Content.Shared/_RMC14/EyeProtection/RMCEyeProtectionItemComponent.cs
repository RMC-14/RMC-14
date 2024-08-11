using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EyeProtection;

/// <summary>
/// For eye protection (e.g. from welding)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSharedEyeProtectionSystem))]
public sealed partial class RMCEyeProtectionItemComponent : Component
{
    /// <summary>
    /// How many seconds to subtract from the status effect. If it's greater than the source
    /// of blindness, do not blind.
    /// </summary>
    [DataField("protectionTime")]
    public TimeSpan ProtectionTime = TimeSpan.FromSeconds(10);

    /// <summary>
    ///  Action ID for toggling welding protection
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleEyeProtection";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    /// <summary>
    /// Which slots can the welding protection status be changed from?
    /// Currently supports only one slot at a time
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags { get; set; } = SlotFlags.EYES;

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
}
