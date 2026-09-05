using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Soak;

public sealed class XenoSoakSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoSoakComponent, XenoSoakActionEvent>(OnXenoSoakAction);

        SubscribeLocalEvent<XenoSoakingDamageComponent, DamageChangedEvent>(OnXenoSoakingDamageChanged);
    }

    private void OnXenoSoakAction(Entity<XenoSoakComponent> xeno, ref XenoSoakActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var soak = EnsureComp<XenoSoakingDamageComponent>(xeno);
        soak.EffectExpiresAt = _timing.CurTime + xeno.Comp.Duration;
        soak.DamageAccumulated = 0;
        Dirty(xeno.Owner, soak);

        var selfMessage = Loc.GetString("rmc-xeno-soak-self");
        _popup.PopupClient(selfMessage, xeno, xeno, PopupType.MediumCaution);

        var others = Filter.PvsExcept(xeno).Recipients;
        foreach (var other in others)
        {
            if (other.AttachedEntity is not { } otherEnt)
            continue;

            var otherMessage = Loc.GetString("rmc-xeno-soak-others", ("xeno", Identity.Name(xeno, EntityManager, otherEnt)));
            _popup.PopupEntity(otherMessage, xeno, otherEnt, PopupType.MediumCaution);
        }

        _aura.GiveAura(xeno, soak.SoakColor, xeno.Comp.Duration);
    }

    private void OnXenoSoakingDamageChanged(Entity<XenoSoakingDamageComponent> xeno, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null || args.DamageDelta.GetTotal() < 0 || !xeno.Comp.Running)
            return;

        xeno.Comp.DamageAccumulated += args.DamageDelta.GetTotal().Float();

        if (xeno.Comp.DamageAccumulated < xeno.Comp.DamageGoal)
            return;

        var amount = -_rmcDamageable.DistributeTypesTotal(xeno.Owner, xeno.Comp.Heal);
        _damage.TryChangeDamage(xeno, amount, origin: xeno, tool: xeno);

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoTailStabEvent>(xeno))
        {
            _action.ClearCooldown(action.AsNullable());
        }

        RemCompDeferred<XenoSoakingDamageComponent>(xeno);
        _aura.GiveAura(xeno, xeno.Comp.RageColor, xeno.Comp.RageDuration);

        if (_net.IsServer)
        {
            var selfMessage = Loc.GetString("rmc-xeno-soak-end-self");
            _popup.PopupEntity(selfMessage, xeno, xeno, PopupType.MediumCaution);

            var others = Filter.PvsExcept(xeno).Recipients;
            foreach (var other in others)
            {
                if (other.AttachedEntity is not { } otherEnt)
                continue;

                var otherMessage = Loc.GetString("rmc-xeno-soak-end-others", ("xeno", Identity.Name(xeno, EntityManager, otherEnt)));
                _popup.PopupEntity(otherMessage, xeno, otherEnt, PopupType.MediumCaution);
            }

        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var soakingQuery = EntityQueryEnumerator<XenoSoakingDamageComponent>();

        while (soakingQuery.MoveNext(out var uid, out var soak))
        {
            if (soak.EffectExpiresAt > time)
                continue;

            RemCompDeferred<XenoSoakingDamageComponent>(uid);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-soak-end-fail"), uid, uid, PopupType.SmallCaution);
        }
    }
}
