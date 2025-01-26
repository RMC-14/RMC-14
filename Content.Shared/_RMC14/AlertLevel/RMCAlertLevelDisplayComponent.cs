using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AlertLevel;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCAlertLevelSystem))]
public sealed partial class RMCAlertLevelDisplayComponent : Component;
