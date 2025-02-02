using Content.Shared._RMC14.AlertLevel;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[Serializable, NetSerializable]
public sealed record MarineControlComputerAlertEvent(NetEntity User, RMCAlertLevels Level);
