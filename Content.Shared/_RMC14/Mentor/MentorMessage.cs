using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

[Serializable, NetSerializable]
public readonly record struct MentorMessage(
    NetUserId Destination,
    string DestinationName,
    NetUserId Author,
    string AuthorName,
    string Text,
    DateTime Time,
    bool IsMentor
);
