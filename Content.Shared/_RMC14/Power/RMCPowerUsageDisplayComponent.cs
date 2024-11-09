using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPowerUsageDisplayComponent : Component
{
    [DataField]
    public string PowerText = "rmc-power-usage-display-defib";
}
