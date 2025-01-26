using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Announce;

[Serializable, NetSerializable]
public sealed record EchoSquadConfirmEvent(NetEntity User, string Message);
