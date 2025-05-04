using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCPatronTier(
    bool ShowOnCredits,
    bool GhostColor,
    bool NamedItems,
    bool Figurines,
    bool LobbyMessage,
    bool RoundEndShoutout,
    string Tier
);
