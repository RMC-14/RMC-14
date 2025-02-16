using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Shared._RMC14.Vendors;
/// <summary>
/// Component used by Specialist Vendors to restrict duplicate kits. Stores successful vends based on Entry int.
/// Since only spec kits need this for porting, all logic specifically uses this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class RMCVendorSpecialistComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<int, int> GlobalSharedVends = new Dictionary<int, int>();
}