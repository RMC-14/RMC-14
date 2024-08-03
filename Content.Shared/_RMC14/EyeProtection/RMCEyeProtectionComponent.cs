using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EyeProtection;

/// <summary>
///     Keeps track of whether eye protection is enabled or not.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RMCSharedEyeProtectionSystem))]
public sealed partial class RMCEyeProtectionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    ///     Is eye protection enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Overlay;
}
