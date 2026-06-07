using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;

/// <summary>
/// Redeemable token that adds points to a matching automated vendor point pool.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVendorPointsTokenComponent : Component
{
    /// <summary>
    /// Vendor point type this token can recharge.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PointsType = "ExperimentalTools";

    /// <summary>
    /// Points granted when redeemed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Points = 45;
}

/// <summary>
/// Raised on a vendor when a user tries to redeem a vendor points token on it.
/// </summary>
[ByRefEvent]
public record struct RMCVendorPointsTokenInteractEvent(EntityUid User, EntityUid Used, bool Handled = false);
