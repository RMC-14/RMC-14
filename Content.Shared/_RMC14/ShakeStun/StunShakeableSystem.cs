using Content.Shared._RMC14.Tackle;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.ShakeStun;

public sealed class StunShakeableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string Stun = "Stun";
    private const string KnockedDown = "KnockedDown";

    public override void Initialize()
    {
        SubscribeLocalEvent<StunShakeableComponent, InteractHandEvent>(OnStunShakeableInteractHand);
    }

    private void OnStunShakeableInteractHand(Entity<StunShakeableComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out StunShakeableUserComponent? shakeableUser))
            return;

        var target = args.Target;
        if (!_statusEffects.HasStatusEffect(target, Stun) &&
            !_statusEffects.HasStatusEffect(target, KnockedDown) &&
            !HasComp<TackledRecentlyComponent>(target))
        {
            return;
        }

        args.Handled = true;

        var time = _timing.CurTime;
        if (time < shakeableUser.LastShake + shakeableUser.Cooldown)
            return;

        shakeableUser.LastShake = time;
        Dirty(user, shakeableUser);

        _statusEffects.TryRemoveStatusEffect(target, "Stun");
        _statusEffects.TryRemoveStatusEffect(target, "KnockedDown");
        RemCompDeferred<TackledRecentlyComponent>(target);

        var userPopup = Loc.GetString("rmc-shake-awake-user", ("target", target));
        _popup.PopupClient(userPopup, target, user);

        var targetPopup = Loc.GetString("rmc-shake-awake-target", ("user", user));
        _popup.PopupEntity(targetPopup, target, target);

        var othersPopup = Loc.GetString("rmc-shake-awake-others", ("user", user), ("target", target));
        var others = Filter.PvsExcept(target).RemovePlayerByAttachedEntity(user);
        _popup.PopupEntity(othersPopup, target, others, true);
    }
}
