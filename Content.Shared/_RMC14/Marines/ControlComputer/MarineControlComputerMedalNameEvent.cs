using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record MarineControlComputerMedalNameEvent(NetEntity Actor, NetEntity? Marine, ProtoId<EntityPrototype> MedalEntityId, string? LastPlayerId = null);
