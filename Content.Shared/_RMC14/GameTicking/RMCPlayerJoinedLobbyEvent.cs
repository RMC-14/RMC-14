using Robust.Shared.Player;

namespace Content.Shared._RMC14.GameTicking;

[ByRefEvent]
public readonly record struct RMCPlayerJoinedLobbyEvent(ICommonSession Player);
