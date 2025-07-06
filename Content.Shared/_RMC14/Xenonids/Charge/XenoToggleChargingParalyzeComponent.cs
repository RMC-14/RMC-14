using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoToggleChargingParalyzeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);
    [DataField, AutoNetworkedField]
    public TimeSpan MaxStageDuration = TimeSpan.FromSeconds(4);
}
