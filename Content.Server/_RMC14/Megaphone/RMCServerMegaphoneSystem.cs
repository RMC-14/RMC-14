using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Megaphone;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Megaphone;

public sealed class RMCServerMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActorComponent, MegaphoneInputEvent>(OnMegaphoneInput);
    }

    private void OnMegaphoneInput(Entity<ActorComponent> ent, ref MegaphoneInputEvent ev)
    {
        if (_timing.ApplyingState)
            return;

        if (string.IsNullOrWhiteSpace(ev.Message))
            return;

        var user = GetEntity(ev.Actor);
        EnsureComp<RMCSpeechBubbleSpecificStyleComponent>(user);
        var userComp = EnsureComp<RMCMegaphoneUserComponent>(user);

        if (TryComp<SpeechComponent>(user, out var speech))
        {
            userComp.OriginalSpeechVerb = speech.SpeechVerb;
            userComp.OriginalSpeechSounds = speech.SpeechSounds;
            userComp.OriginalSuffixSpeechVerbs = speech.SuffixSpeechVerbs;

            speech.SpeechVerb = userComp.SpeechVerb;
            speech.SpeechSounds = userComp.MegaphoneSpeechSound;
            speech.SuffixSpeechVerbs = userComp.SuffixSpeechVerbs;
            Dirty(user, speech);

            // Send a message to the chat
            _chat.TrySendInGameICMessage(user, ev.Message, InGameICChatType.Speak, false);

            // Restore the original speech settings
            speech.SpeechVerb = userComp.OriginalSpeechVerb ?? "Default";
            speech.SpeechSounds = userComp.OriginalSpeechSounds;
            speech.SuffixSpeechVerbs = userComp.OriginalSuffixSpeechVerbs ?? new();
            Dirty(user, speech);
        }

        RemComp<RMCSpeechBubbleSpecificStyleComponent>(user);
        RemComp<RMCMegaphoneUserComponent>(user);
    }
}
