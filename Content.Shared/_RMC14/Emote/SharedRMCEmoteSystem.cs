using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Emote;

public abstract class SharedRMCEmoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private TimeSpan _emoteCooldown;

    private readonly float _interactRange = 1f;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCHandEmotesComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCHandEmotesComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<RMCHandEmotesComponent, MoveInputEvent>(OnMove);

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

    private void OnInteractHand(Entity<RMCHandEmotesComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (user == args.Target || !TryComp(user, out RMCHandEmotesComponent? compUser))
            return;

        if (!ent.Comp.Active || compUser.Active)
            return;

        if (user != ent.Comp.Target)
            return;

        if (ent.Comp.State == RMCHandsEmoteState.Tailswipe && !HasComp<XenoComponent>(user))
            return;

        if (!_interaction.InRangeUnobstructed(user, args.Target, _interactRange))
        {
            var msg = Loc.GetString("rmc-hands-emotes-get-closer");
            _popup.PopupClient(msg, user, user);
            return;
        }

        args.Handled = true;
        PerformEmote(ent, (user, compUser));
    }

    private void OnGetInteractionVerbs(Entity<RMCHandEmotesComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        if (!TryComp<RMCHandEmotesComponent>(args.User, out var selfComp))
            return;

        if (ent.Comp.Active || selfComp.Active || ent.Owner == args.User)
            return;

        var user = args.User;

        if (HasComp<XenoComponent>(user) && HasComp<XenoComponent>(ent.Owner))
        {
            args.Verbs.Add(new()
            {
                Act = () => AttemptEmote((user, selfComp), ent, RMCHandsEmoteState.Tailswipe),
                Text = Loc.GetString("rmc-hands-emotes-tailswipe-perform"),
                Priority = -27,
                Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Effects/emotes.rsi"), "emote_tailswipe")
            });
        }
        else if (!HasComp<XenoComponent>(user) && !HasComp<XenoComponent>(ent.Owner))
        {
            args.Verbs.Add(new()
            {
                Act = () => AttemptEmote((user, selfComp), ent, RMCHandsEmoteState.Fistbump),
                Text = Loc.GetString("rmc-hands-emotes-fistbump-perform"),
                Priority = -25,
                Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Effects/emotes.rsi"), "emote_fistbump")
            });

            args.Verbs.Add(new()
            {
                Act = () => AttemptEmote((user, selfComp), ent, RMCHandsEmoteState.Highfive),
                Text = Loc.GetString("rmc-hands-emotes-highfive-perform"),
                Priority = -26,
                Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Effects/emotes.rsi"), "emote_highfive")
            });

            args.Verbs.Add(new()
            {
                Act = () => AttemptEmote((user, selfComp), ent, RMCHandsEmoteState.Hug),
                Text = Loc.GetString("rmc-hands-emotes-hug-perform"),
                Priority = -28,
                Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Effects/emotes.rsi"), "emote_hug")
            });
        }
    }

    private void OnMove(Entity<RMCHandEmotesComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        CancelHandEmotes(ent);
    }

    public void AttemptEmote(Entity<RMCHandEmotesComponent> ent, Entity<RMCHandEmotesComponent> target, RMCHandsEmoteState state)
    {
        var effect = state switch
        {
            RMCHandsEmoteState.Fistbump => ent.Comp.FistBumpEffect,
            RMCHandsEmoteState.Highfive => ent.Comp.HighFiveEffect,
            RMCHandsEmoteState.Tailswipe => ent.Comp.TailSwipeEffect,
            RMCHandsEmoteState.Hug => ent.Comp.HugEffect,
            _ => throw new ArgumentOutOfRangeException()
        };

        var popup = state switch
        {
            RMCHandsEmoteState.Fistbump => Loc.GetString("rmc-hands-emotes-fistbump-attempt", ("ent", ent), ("target", target)),
            RMCHandsEmoteState.Highfive => Loc.GetString("rmc-hands-emotes-highfive-attempt", ("ent", ent), ("target", target)),
            RMCHandsEmoteState.Tailswipe => Loc.GetString("rmc-hands-emotes-tailswipe-attempt", ("ent", ent), ("target", target)),
            RMCHandsEmoteState.Hug => Loc.GetString("rmc-hands-emotes-hug-attempt", ("ent", ent), ("target", target)),
            _ => throw new ArgumentOutOfRangeException()
        };

        var popupSelf = state switch
        {
            RMCHandsEmoteState.Fistbump => Loc.GetString("rmc-hands-emotes-fistbump-attempt-self", ("ent", ent), ("target", target)),
            RMCHandsEmoteState.Highfive => Loc.GetString("rmc-hands-emotes-highfive-attempt-self", ("ent", ent), ("target", target)),
            RMCHandsEmoteState.Tailswipe => Loc.GetString("rmc-hands-emotes-tailswipe-attempt-self", ("ent", ent), ("target", target)),
            RMCHandsEmoteState.Hug => Loc.GetString("rmc-hands-emotes-hug-attempt-self", ("ent", ent), ("target", target)),
            _ => throw new ArgumentOutOfRangeException()
        };

        ent.Comp.Active = true;
        ent.Comp.Target = target.Owner;
        ent.Comp.LeaveHangingAt = _timing.CurTime + ent.Comp.LeftHangingDelay;
        ent.Comp.State = state;

        if (_net.IsServer)
            ent.Comp.SpawnedEffect = SpawnAttachedTo(effect, ent.Owner.ToCoordinates());

        _popup.PopupPredicted(popupSelf, popup, ent.Owner, ent.Owner, PopupType.Medium);
        Dirty(ent);
    }

    public void CancelHandEmotes(Entity<RMCHandEmotesComponent> ent)
    {
        ent.Comp.Target = null;
        ent.Comp.Active = false;

        if (_net.IsServer && ent.Comp.SpawnedEffect != null)
            QueueDel(ent.Comp.SpawnedEffect);

        ent.Comp.SpawnedEffect = null;

        Dirty(ent);
    }

    public void PerformEmote(Entity<RMCHandEmotesComponent> ent, Entity<RMCHandEmotesComponent> target)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var uid = ent.Owner;
        var targetUid = target.Owner;

        var state = ent.Comp.State;

        var sound = state switch
        {
            RMCHandsEmoteState.Fistbump => ent.Comp.FistBumpSound,
            RMCHandsEmoteState.Highfive => ent.Comp.HighFiveSound,
            RMCHandsEmoteState.Hug => ent.Comp.HugSound,
            RMCHandsEmoteState.Tailswipe => ent.Comp.TailSwipeSound,
            _ => throw new ArgumentOutOfRangeException()
        };

        var popup = state switch
        {
            RMCHandsEmoteState.Fistbump => Loc.GetString("rmc-hands-emotes-fistbump", ("ent", uid), ("target", targetUid)),
            RMCHandsEmoteState.Highfive => Loc.GetString("rmc-hands-emotes-highfive", ("ent", uid), ("target", targetUid)),
            RMCHandsEmoteState.Hug => Loc.GetString("rmc-hands-emotes-hug", ("ent", uid), ("target", targetUid)),
            RMCHandsEmoteState.Tailswipe => Loc.GetString("rmc-hands-emotes-tailswipe", ("ent", uid), ("target", targetUid)),
            _ => throw new ArgumentOutOfRangeException()
        };

        var popupSelf = state switch
        {
            RMCHandsEmoteState.Fistbump => Loc.GetString("rmc-hands-emotes-fistbump-self", ("ent", uid), ("target", targetUid)),
            RMCHandsEmoteState.Highfive => Loc.GetString("rmc-hands-emotes-highfive-self", ("ent", uid), ("target", targetUid)),
            RMCHandsEmoteState.Hug => Loc.GetString("rmc-hands-emotes-hug-self", ("ent", uid), ("target", targetUid)),
            RMCHandsEmoteState.Tailswipe => Loc.GetString("rmc-hands-emotes-tailswipe-self", ("ent", uid), ("target", targetUid)),
            _ => throw new ArgumentOutOfRangeException()
        };

        var popupSelfTarget = state switch
        {
            RMCHandsEmoteState.Fistbump => Loc.GetString("rmc-hands-emotes-fistbump-self", ("ent", targetUid), ("target", uid)),
            RMCHandsEmoteState.Highfive => Loc.GetString("rmc-hands-emotes-highfive-self", ("ent", targetUid), ("target", uid)),
            RMCHandsEmoteState.Hug => Loc.GetString("rmc-hands-emotes-hug-self", ("ent", targetUid), ("target", uid)),
            RMCHandsEmoteState.Tailswipe => Loc.GetString("rmc-hands-emotes-tailswipe-self", ("ent", targetUid), ("target", uid)),
            _ => throw new ArgumentOutOfRangeException()
        };

        _popup.PopupClient(popupSelf, uid, uid, PopupType.Medium);
        _popup.PopupClient(popupSelfTarget, targetUid, targetUid, PopupType.Medium);

        _melee.DoLunge(targetUid, uid);

        _rotate.TryFaceCoordinates(uid, _transform.GetMapCoordinates(targetUid).Position);
        _rotate.TryFaceCoordinates(targetUid, _transform.GetMapCoordinates(uid).Position);

        if (_net.IsServer)
        {
            var others = Filter.PvsExcept(uid).RemovePlayerByAttachedEntity(targetUid);
            _popup.PopupEntity(popup, uid, others, true);

            _audio.PlayPvs(sound, uid);
            _melee.DoLunge(uid, targetUid);
        }

        CancelHandEmotes(ent);
        CancelHandEmotes(target);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var handQuery = EntityQueryEnumerator<RMCHandEmotesComponent, TransformComponent>();
        while (handQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Active)
                continue;

            if (time < comp.LeaveHangingAt)
                continue;

            CancelHandEmotes((uid, comp));

            var leaveHangingMessage = Loc.GetString("rmc-hands-emotes-left-hanging");
            _popup.PopupEntity(leaveHangingMessage, uid, uid, PopupType.SmallCaution);
        }
    }
}
