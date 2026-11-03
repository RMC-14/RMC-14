using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.ReagentScanner;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCReagentScannerSystem))]
public sealed partial class RMCReagentScannerComponent : Component
{
    [DataField]
    public bool Details;
}
