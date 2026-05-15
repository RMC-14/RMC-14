using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCReactorPoweredLightComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;
}
