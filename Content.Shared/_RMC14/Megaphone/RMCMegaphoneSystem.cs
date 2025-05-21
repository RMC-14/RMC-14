using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Content.Shared._RMC14.Chat;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMegaphoneComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<RMCMegaphoneComponent, RMCToggleMegaphoneActionEvent>(OnToggleAction);
        SubscribeLocalEvent<RMCMegaphoneComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<RMCMegaphoneUserComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    private void OnGetItemActions(Entity<RMCMegaphoneComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);

        if (ent.Comp.Enabled)
        {
            EnsureComp<RMCSpeechBubbleSpecificStyleComponent>(args.User);
            EnsureComp<RMCMegaphoneUserComponent>(args.User);
        }
    }

    private void OnToggleAction(Entity<RMCMegaphoneComponent> ent, ref RMCToggleMegaphoneActionEvent args)
    {
        var user = args.Performer;
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        _audio.PlayLocal(ent.Comp.ToggleSound, ent, user);
        _actions.SetToggled(ent.Comp.Action, ent.Comp.Enabled);

        if (ent.Comp.Enabled)
        {
            EnsureComp<RMCSpeechBubbleSpecificStyleComponent>(user);
            EnsureComp<RMCMegaphoneUserComponent>(user);
        }
        else
        {
            RemComp<RMCSpeechBubbleSpecificStyleComponent>(user);
            RemComp<RMCMegaphoneUserComponent>(user);
        }
    }

    private void OnUnequipped(Entity<RMCMegaphoneComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_hands.IsHolding(args.User, ent.Owner))
            return;

        if (ent.Comp.Enabled)
        {
            RemComp<RMCSpeechBubbleSpecificStyleComponent>(args.User);
            RemComp<RMCMegaphoneUserComponent>(args.User);
        }

        if (ent.Comp.DeactivateOnUnequip)
        {
            ent.Comp.Enabled = false;
            Dirty(ent);
            _actions.SetToggled(ent.Comp.Action, ent.Comp.Enabled);
        }

        _actions.RemoveAction(args.User, ent.Comp.Action);
    }

    private void OnSpeakAttempt(Entity<RMCMegaphoneUserComponent> ent, ref SpeakAttemptEvent args)
    {
        _audio.PlayPredicted(ent.Comp.OnSpeakSound, ent, args.Uid);
    }
}
