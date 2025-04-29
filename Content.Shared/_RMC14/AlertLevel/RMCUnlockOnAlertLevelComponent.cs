using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AlertLevel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCAlertLevelSystem))]
public sealed partial class RMCUnlockOnAlertLevelComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCAlertLevels Level = RMCAlertLevels.Red;
}
