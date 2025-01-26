using Content.Shared.Database;
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
