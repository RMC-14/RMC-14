using Content.Shared._RMC14.CCVar;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Emote;

public abstract class SharedRMCEmoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _emoteCooldown;

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCEmoteCooldownSeconds, v => _emoteCooldown = TimeSpan.FromSeconds(v), true);
    }

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

    public bool TryEmote(Entity<EmoteCooldownComponent?> cooldown)
    {
        if (!Resolve(cooldown, ref cooldown.Comp, false))
            return true;

        var time = _timing.CurTime;
        if (time < cooldown.Comp.NextEmote)
            return false;

        cooldown.Comp.NextEmote = time + _emoteCooldown;
        Dirty(cooldown);
        return true;
    }

    public void ResetCooldown(Entity<EmoteCooldownComponent?> cooldown)
    {
        if (!Resolve(cooldown, ref cooldown.Comp, false))
            return;

        cooldown.Comp.NextEmote = _timing.CurTime + _emoteCooldown;
        Dirty(cooldown);
    }
}
