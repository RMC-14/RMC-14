using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.SmartFridge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCSmartFridgeSystem))]
public sealed partial class RMCSmartFridgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_smart_fridge";
}
