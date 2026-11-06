using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPowerNetComponent : Component
{
    [DataField, AutoNetworkedField]
    public string PowerNet = "default";

    [DataField, AutoNetworkedField]
    public RMCPowerNetworkStats Stats;
}
