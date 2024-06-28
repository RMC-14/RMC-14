using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMVendorBundleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Bundle = new();
}
