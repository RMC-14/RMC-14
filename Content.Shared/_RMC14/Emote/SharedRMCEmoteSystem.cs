using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Emote;

public abstract class SharedRMCEmoteSystem : EntitySystem
{
    public virtual void TryEmoteWithChat(
        EntityUid source,
        ProtoId<EmotePrototype> emote,
        bool hideLog = false,
        string? nameOverride = null,
        bool ignoreActionBlocker = false,
        bool forceEmote = false,
        TimeSpan? cooldown = null)
    {
    }
}
