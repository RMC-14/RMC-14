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
    ///     Radius of full sight restriction in tiles counted from screen edge
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("impairFull"), AutoNetworkedField]
    public float ImpairFull = 3.0f;
    /// <summary>
    ///     Radius of partial sight restriction in tiles counted from edge of full sight restriction
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("impairPartial"), AutoNetworkedField]
    public float ImpairPartial = 2.0f;

    /// <summary>
    ///     Alpha component of full sight restriction
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("alphaOuter"), AutoNetworkedField]
    public float AlphaOuter = 1.0f;
    /// <summary>
    ///     Alpha component of unrestricted sight; the alpha of partial sight restriction is a gradient between this and AlphaOuter
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("alphaInner"), AutoNetworkedField]
    public float AlphaInner = 0.0f;
}
