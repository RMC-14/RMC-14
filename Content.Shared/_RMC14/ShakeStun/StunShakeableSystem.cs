using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.ShakeStun;

public sealed class StunShakeableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StunShakeableComponent, InteractHandEvent>(OnStunShakeableInteractHand);
    }

    private void OnStunShakeableInteractHand(Entity<StunShakeableComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (!HasComp<StunShakeableUserComponent>(user))
            return;

        var target = args.Target;
        var any = _statusEffects.TryRemoveStatusEffect(target, "Stun");
        if (_statusEffects.TryRemoveStatusEffect(target, "KnockedDown"))
            any = true;

        if (!any)
            return;

        var userPopup = Loc.GetString("rmc-shake-awake-user", ("target", target));
        _popup.PopupClient(userPopup, target, user);

        var targetPopup = Loc.GetString("rmc-shake-awake-target", ("user", user));
        _popup.PopupEntity(targetPopup, target, target);

        var othersPopup = Loc.GetString("rmc-shake-awake-others", ("user", user), ("target", target));
        var others = Filter.PvsExcept(target).RemovePlayerByAttachedEntity(user);
        _popup.PopupEntity(othersPopup, target, others, true);
    }
}
