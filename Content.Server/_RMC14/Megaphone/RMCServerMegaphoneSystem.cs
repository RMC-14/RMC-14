using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Megaphone;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Speech;
using Robust.Server.Console;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Megaphone;

public sealed class RMCServerMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActorComponent, MegaphoneInputEvent>(OnMegaphoneInput);
        SubscribeLocalEvent<RMCMegaphoneUserComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);
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
        userComp.VoiceRange = ev.VoiceRange;
        userComp.Amplifying = ev.Amplifying;
        Dirty(user, userComp);

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

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        if (!TryComp<RMCMegaphoneUserComponent>(ev.Source, out var megaphoneUser))
            return;

        if (!megaphoneUser.Amplifying)
            return;

        var megaphoneRange = megaphoneUser.VoiceRange;
        if (ev.VoiceRange >= megaphoneRange)
            return;

        var sourceTransform = Transform(ev.Source);
        var sourcePos = _transform.GetWorldPosition(sourceTransform);
        var xforms = GetEntityQuery<TransformComponent>();
        var ghostHearing = GetEntityQuery<GhostHearingComponent>();

        // Add recipients within megaphone range but outside normal range
        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            if (ev.Recipients.ContainsKey(player))
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceTransform.MapID)
                continue;

            var recipientPos = _transform.GetWorldPosition(transformEntity, xforms);
            var distance = (sourcePos - recipientPos).Length();

            // Add if within megaphone range but outside normal range
            if (distance < megaphoneRange && distance >= ev.VoiceRange)
            {
                var observer = ghostHearing.HasComponent(playerEntity);
                ev.Recipients.TryAdd(player, new ICChatRecipientData(distance, observer));
            }
        }
    }
}
