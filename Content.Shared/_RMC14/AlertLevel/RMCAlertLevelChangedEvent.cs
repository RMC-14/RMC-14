namespace Content.Shared._RMC14.AlertLevel;

[ByRefEvent]
public readonly record struct RMCAlertLevelChangedEvent(RMCAlertLevels Level);
