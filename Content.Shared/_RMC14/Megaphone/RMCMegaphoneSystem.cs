using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Chat;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMegaphoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCMegaphoneComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<RMCMegaphoneComponent, GotEquippedHandEvent>(OnEquipped);
    }

    private void OnUseInHand(Entity<RMCMegaphoneComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        Toggle(ent, args.User);
    }

    private void OnEquipped(Entity<RMCMegaphoneComponent> ent, ref GotEquippedHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Enabled)
        {
            EnsureComp<RMCSpeechBubbleSpecificStyleComponent>(args.User);
            var userComp = EnsureComp<RMCMegaphoneUserComponent>(args.User);
            if (TryComp<SpeechComponent>(args.User, out var speech))
            {
                userComp.OriginalSpeechVerb = speech.SpeechVerb;
                userComp.OriginalSpeechSounds = speech.SpeechSounds;
                userComp.OriginalSuffixSpeechVerbs = speech.SuffixSpeechVerbs;
                speech.SpeechVerb = userComp.SpeechVerb;
                speech.SpeechSounds = userComp.MegaphoneSpeechSound;
                speech.SuffixSpeechVerbs = userComp.SuffixSpeechVerbs;
                Dirty(args.User, speech);
            }
        }
    }

    private void OnUnequipped(Entity<RMCMegaphoneComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (_hands.IsHolding(args.User, ent.Owner))
            return;

        if (ent.Comp.Enabled)
        {
            RemComp<RMCSpeechBubbleSpecificStyleComponent>(args.User);
            if (TryComp<RMCMegaphoneUserComponent>(args.User, out var userComp) &&
                TryComp<SpeechComponent>(args.User, out var speech))
            {
                speech.SpeechVerb = userComp.OriginalSpeechVerb ?? "Default";
                speech.SpeechSounds = userComp.OriginalSpeechSounds;
                speech.SuffixSpeechVerbs = userComp.OriginalSuffixSpeechVerbs ?? new();
                Dirty(args.User, speech);
            }
            RemComp<RMCMegaphoneUserComponent>(args.User);
        }

        if (ent.Comp.DeactivateOnUnequip)
        {
            ent.Comp.Enabled = false;
            Dirty(ent);
            UpdateAppearance(ent);
        }
    }

    private void Toggle(Entity<RMCMegaphoneComponent> ent, EntityUid user)
    {
        if (_timing.ApplyingState)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        _audio.PlayLocal(ent.Comp.ToggleSound, ent, user);
        UpdateAppearance(ent);

        if (ent.Comp.Enabled)
        {
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
            }
            _popup.PopupClient(Loc.GetString("rmc-megaphone-enabled"), user, user);
        }
        else
        {
            RemComp<RMCSpeechBubbleSpecificStyleComponent>(user);
            if (TryComp<RMCMegaphoneUserComponent>(user, out var userComp) &&
                TryComp<SpeechComponent>(user, out var speech))
            {
                speech.SpeechVerb = userComp.OriginalSpeechVerb ?? "Default";
                speech.SpeechSounds = userComp.OriginalSpeechSounds;
                speech.SuffixSpeechVerbs = userComp.OriginalSuffixSpeechVerbs ?? new();
                Dirty(user, speech);
            }
            RemComp<RMCMegaphoneUserComponent>(user);
            _popup.PopupClient(Loc.GetString("rmc-megaphone-disabled"), user, user);
        }
    }

    private void UpdateAppearance(Entity<RMCMegaphoneComponent> ent)
    {
        _appearance.SetData(ent, MegaphoneVisuals.Light, ent.Comp.Enabled ? MegaphoneLightState.On : MegaphoneLightState.Off);
    }
}
