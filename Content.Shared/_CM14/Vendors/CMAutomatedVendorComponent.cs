using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMAutomatedVendorComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<CMVendorSection> Sections = new();
}
