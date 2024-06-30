using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCPatronFull(
    SharedRMCPatronTier? Tier,
    bool Linked,
    SharedRMCLobbyMessage? LobbyMessage,
    SharedRMCRoundEndShoutouts? RoundEndShoutout
);
