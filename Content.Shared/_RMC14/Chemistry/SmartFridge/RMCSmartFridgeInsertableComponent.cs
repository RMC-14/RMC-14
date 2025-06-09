using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.SmartFridge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCSmartFridgeSystem))]
public sealed partial class RMCSmartFridgeInsertableComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Category = "Other";
}
