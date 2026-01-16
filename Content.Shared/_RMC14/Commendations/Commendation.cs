using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Commendations;

[Serializable, NetSerializable]
public readonly record struct Commendation(
    string Giver,
    string Receiver,
    string Name,
    string Text,
    CommendationType Type,
    int Round
);

/// <summary>
/// Extended commendation data stored during the round.
/// Contains additional information like prototype ID and receiver entity that is not sent to database.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct RoundCommendationEntry(
    Commendation Commendation,
    ProtoId<EntityPrototype>? CommendationPrototypeId,
    NetEntity? ReceiverEntity,
    string? ReceiverLastPlayerId
);
