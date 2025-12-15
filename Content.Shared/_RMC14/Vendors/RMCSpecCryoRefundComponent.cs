using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSpecCryoRefundComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityUid Vendor;

    [DataField(required: true), AutoNetworkedField]
    public int Entry;
}
