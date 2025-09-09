using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record MarineControlComputerMedalMarineEvent(NetEntity Actor, NetEntity? Marine, string? LastPlayerId = null);
