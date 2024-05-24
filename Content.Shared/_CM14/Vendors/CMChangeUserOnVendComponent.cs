using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Vendors;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMChangeUserOnVendComponent : Component
{
    [DataField]
    public ComponentRegistry? AddComponents;
}
