using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCRoundEndShoutouts(string? Marine, string? Xeno)
{
    public const int CharacterLimit = 50;
}
