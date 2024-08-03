using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EyeProtection;

/// <summary>
/// For eye protection (e.g. from welding)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
//[Access(typeof(RMCSharedEyeProtectionSystem))]
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
    public EntProtoId ActionId = "CMActionToggleEyeProtection";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    /// <summary>
    ///  Is welding protection enabled?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("toggled"), AutoNetworkedField]
    public bool Toggled = true;
}
