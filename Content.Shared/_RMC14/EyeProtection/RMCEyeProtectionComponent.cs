using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.EyeProtection;

/// <summary>
///     Component responsible for restricting vision when eye protection is enabled.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RMCSharedEyeProtectionSystem))]
public sealed partial class RMCEyeProtectionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    [DataField, AutoNetworkedField]
    public bool Overlay;

    /// <summary>
    ///     The strength of the sight restriction
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("zoom"), AutoNetworkedField]
    public float Zoom = 0.20f;
}
