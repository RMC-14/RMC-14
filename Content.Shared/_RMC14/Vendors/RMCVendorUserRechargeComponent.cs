using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVendorUserRechargeSystem), typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class RMCVendorUserRechargeComponent : Component
{

    [DataField, AutoNetworkedField]
    public int MaxPoints;

    [DataField, AutoNetworkedField]
    public int PointsPerUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan TimePerUpdate = TimeSpan.FromMinutes(1);

    [AutoNetworkedField]
    public TimeSpan LastUpdate = TimeSpan.Zero;
}
