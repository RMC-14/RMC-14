using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Emote;

public abstract class SharedRMCEmoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

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

    public bool CanEmote(Entity<EmoteCooldownComponent?> cooldown)
    {
        if (!Resolve(cooldown, ref cooldown.Comp, false))
            return true;

        return _timing.CurTime >= cooldown.Comp.NextEmote;
    }

    public void ResetCooldown(Entity<EmoteCooldownComponent?> cooldown)
    {
        if (!Resolve(cooldown, ref cooldown.Comp, false))
            return;

        cooldown.Comp.NextEmote = _timing.CurTime;
        Dirty(cooldown);
    }
}
