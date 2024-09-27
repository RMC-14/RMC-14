using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Emote;

public sealed class RMCEmoteSystem : SharedRMCEmoteSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
