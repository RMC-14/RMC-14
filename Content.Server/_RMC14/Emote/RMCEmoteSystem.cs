using Content.Server.Chat.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Emote;

public sealed class RMCEmoteSystem : SharedRMCEmoteSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _emoteCooldown;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmoteCooldownComponent, EmoteEvent>(OnEmoteCooldownEmote);
        SubscribeLocalEvent<EmoteCooldownComponent, ScreamActionEvent>(OnCooldownScreamAction, before: [typeof(VocalSystem)]);

        Subs.CVar(_config, RMCCVars.RMCEmoteCooldownSeconds, v => _emoteCooldown = TimeSpan.FromSeconds(v), true);
    }

    private void OnCooldownScreamAction(Entity<EmoteCooldownComponent> ent, ref ScreamActionEvent args)
    {
        // always allow off-cooldown scream action emote
        ResetCooldown((ent, ent));
    }

    private void OnEmoteCooldownEmote(Entity<EmoteCooldownComponent> ent, ref EmoteEvent args)
    {
        ent.Comp.NextEmote = _timing.CurTime + _emoteCooldown;
        Dirty(ent);
    }

    public override void TryEmoteWithChat(
        EntityUid source,
        ProtoId<EmotePrototype> emote,
        bool hideLog = false,
        string? nameOverride = null,
        bool ignoreActionBlocker = false,
        bool forceEmote = false,
        TimeSpan? cooldown = null)
    {
        var recently = EnsureComp<RecentlyEmotedComponent>(source);
        var time = _timing.CurTime;
        if (recently.Emotes.TryGetValue(emote, out var next) &&
            time < next)
        {
            return;
        }

        recently.Emotes[emote] = time + cooldown ?? recently.Cooldown;
        _chat.TryEmoteWithChat(
            source,
            emote,
            ChatTransmitRange.Normal,
            hideLog,
            nameOverride,
            ignoreActionBlocker,
            forceEmote
        );
    }
}
