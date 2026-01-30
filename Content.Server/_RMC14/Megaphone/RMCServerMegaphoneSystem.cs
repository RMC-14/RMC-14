using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Megaphone;
using Content.Shared.Speech;
using Robust.Server.Console;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Megaphone;

public sealed class RMCServerMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActorComponent, MegaphoneInputEvent>(OnMegaphoneInput);
        SubscribeLocalEvent<RMCMegaphoneUserComponent, EntitySpokeEvent>(OnEntitySpoke);
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

            // Send a message using the say command
            var session = ent.Comp.PlayerSession;
            _console.ExecuteCommand(session, $"say \"{CommandParsing.Escape(ev.Message)}\"");

            // Restore the original speech settings
            speech.SpeechVerb = userComp.OriginalSpeechVerb ?? "Default";
            speech.SpeechSounds = userComp.OriginalSpeechSounds;
            speech.SuffixSpeechVerbs = userComp.OriginalSuffixSpeechVerbs ?? new();
            Dirty(user, speech);
        }
    }

    private void OnEntitySpoke(Entity<RMCMegaphoneUserComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Channel != null)
            return;

        // Remove components after the message is sent
        RemComp<RMCMegaphoneUserComponent>(ent);
        RemComp<RMCSpeechBubbleSpecificStyleComponent>(ent);
    }
}
