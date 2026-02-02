using Content.Shared._RMC14.Dialog;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.ControlComputer;

[ByRefEvent]
[Serializable, NetSerializable]
public sealed record MarineControlComputerMedalMessageEvent(
    NetEntity Actor,
    NetEntity? Marine,
    string Name,
    ProtoId<EntityPrototype> CommendationPrototypeId,
    string Message = "",
    string? LastPlayerId = null
) : DialogInputEvent(Message);
