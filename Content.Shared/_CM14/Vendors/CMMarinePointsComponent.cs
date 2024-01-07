using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMMarinePointsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Points;
}
