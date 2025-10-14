using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Standing;
using Content.Shared._RMC14.Tackle;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.ShakeStun;

public sealed class StunShakeableSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCStandingSystem _rmcStanding = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<StatusEffectPrototype> Stun = "Stun";
    private static readonly ProtoId<StatusEffectPrototype> KnockedDown = "KnockedDown";
    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";

    public override void Initialize()
    {
        SubscribeLocalEvent<StunShakeableComponent, InteractHandEvent>(OnStunShakeableInteractHand,
            before: [typeof(InteractionPopupSystem)]);
    }

    private void OnStunShakeableInteractHand(Entity<StunShakeableComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (user == args.Target ||
            !TryComp(user, out StunShakeableUserComponent? shakeableUser))
        {
            return;
        }

        var target = args.Target;
        var rest = CompOrNull<RMCRestComponent>(target);
        if (!_statusEffects.HasStatusEffect(target, Stun) &&
            !_statusEffects.HasStatusEffect(target, KnockedDown) &&
            !_statusEffects.HasStatusEffect(target, Unconscious) &&
            !HasComp<TackledRecentlyByComponent>(target) &&
            (rest == null || !rest.Resting))
        {
            return;
        }

        args.Handled = true;

        var time = _timing.CurTime;
        if (time < shakeableUser.LastShake + shakeableUser.Cooldown)
            return;

        shakeableUser.LastShake = time;
        Dirty(user, shakeableUser);

        //They fall back down instantly in stam crit
        if (TryComp<RMCStaminaComponent>(ent, out var stamina) && stamina.Level >= 4)
        {
            _popup.PopupClient(Loc.GetString("rmc-shake-awake-stamina", ("target", target)), target, user);
            return;
        }

        _rmcStanding.SetRest(target, false);

        _statusEffects.TryRemoveTime(target, Stun, ent.Comp.DurationRemoved);
        _statusEffects.TryRemoveTime(target, KnockedDown, ent.Comp.DurationRemoved);
        _statusEffects.TryRemoveTime(target, Unconscious, ent.Comp.DurationRemoved);
        RemCompDeferred<TackledRecentlyByComponent>(target);

        var userPopup = Loc.GetString("rmc-shake-awake-user", ("target", target));
        _popup.PopupClient(userPopup, target, user);

        var targetPopup = Loc.GetString("rmc-shake-awake-target", ("user", user));
        _popup.PopupEntity(targetPopup, target, target);

        if (_net.IsServer)
            _audio.PlayEntity(ent.Comp.ShakeSound, Filter.Pvs(target), target, false);

        var othersPopup = Loc.GetString("rmc-shake-awake-others", ("user", user), ("target", target));
        var others = Filter.PvsExcept(target).RemovePlayerByAttachedEntity(user);
        _popup.PopupEntity(othersPopup, target, others, true);

        _adminLogs.Add(LogType.RMCStunShake, $"{ToPrettyString(user)} shook {target} out of a stun.");
    }
}
