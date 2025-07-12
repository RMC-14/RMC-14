using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record MarineControlComputerMedalNameEvent(NetEntity Actor, NetEntity? Marine, string Name, string? LastPlayerId = null);
