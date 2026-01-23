using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Megaphone;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Ghost;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;
using Robust.Server.Console;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> LeadershipSkill = "RMCSkillLeadership";
    private static readonly EntProtoId HushedStatusEffect = "RMCStatusEffectHushed";

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
        userComp.VoiceRangeMultiplier = ev.VoiceRangeMultiplier;
        userComp.MaxHushedEffectRange = ev.MaxHushedEffectRange;
        userComp.CurrentHushedEffectRange = Math.Min(ev.CurrentHushedEffectRange, ev.MaxHushedEffectRange);
        userComp.HushedEffectDuration = ev.HushedEffectDuration;
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

        var megaphoneRange = ev.VoiceRange * megaphoneUser.VoiceRangeMultiplier;
        var hushedRange = Math.Min(megaphoneRange, Math.Min(megaphoneUser.CurrentHushedEffectRange, megaphoneUser.MaxHushedEffectRange));

        var sourceTransform = Transform(ev.Source);
        var sourcePos = _transform.GetWorldPosition(sourceTransform);
        var xforms = GetEntityQuery<TransformComponent>();

        // Check if we should apply hushed effect (user has leadership skill or is squad leader and hushed range is not zero)
        var hasLeadership = _skills.HasSkill(ev.Source, LeadershipSkill, 1) || HasComp<SquadLeaderComponent>(ev.Source);
        var shouldApplyHushed = megaphoneUser.CurrentHushedEffectRange > 0 &&
                                 megaphoneUser.HushedEffectDuration > TimeSpan.Zero &&
                                 hasLeadership;

        // Get source faction for friendly check (only needed for hushed effect)
        var hasSourceFaction = shouldApplyHushed && _gunIFF.TryGetFaction(ev.Source, out var sourceFaction);

        // Add recipients within megaphone range but outside normal range
        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceTransform.MapID)
                continue;

            var recipientPos = _transform.GetWorldPosition(transformEntity, xforms);
            var distance = (sourcePos - recipientPos).Length();
            var observer = HasComp<GhostComponent>(playerEntity);

            // Add if within megaphone range but outside normal range
            if (distance < megaphoneRange && distance >= ev.VoiceRange && !ev.Recipients.ContainsKey(player))
                ev.Recipients.TryAdd(player, new ICChatRecipientData(distance, observer));

            if (shouldApplyHushed && distance < hushedRange)
            {
                if (observer)
                    continue;

                // Check if recipient is friendly (same faction)
                if (!hasSourceFaction || !_gunIFF.IsInFaction(playerEntity, sourceFaction))
                    continue;

                if (!_skills.HasSkill(playerEntity, LeadershipSkill, 1) && !HasComp<SquadLeaderComponent>(playerEntity))
                    _statusEffects.TryUpdateStatusEffectDuration(playerEntity, HushedStatusEffect, megaphoneUser.HushedEffectDuration);
            }
        }
    }
}
