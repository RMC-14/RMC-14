using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Actions;
using Content.Shared.Damage;
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

        _popup.PopupPredicted(Loc.GetString("rmc-xeno-soak-self"), Loc.GetString("rmc-xeno-soak-others", ("xeno", xeno)), xeno, xeno, PopupType.MediumCaution);
        _aura.GiveAura(xeno, soak.SoakColor, xeno.Comp.Duration);
    }

    private void OnXenoSoakingDamageChanged(Entity<XenoSoakingDamageComponent> xeno, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null || args.DamageDelta.GetTotal() < 0)
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
            _popup.PopupEntity(Loc.GetString("rmc-xeno-soak-end-self"), xeno, xeno, PopupType.MediumCaution);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-soak-end-others", ("xeno", xeno)), xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);
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
