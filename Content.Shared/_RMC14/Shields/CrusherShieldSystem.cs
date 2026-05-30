using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Explosion;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Shields;

public sealed partial class CrusherShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly XenoShieldSystem _shield = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrusherShieldComponent, DamageModifyAfterResistEvent>(OnDamage, before: [typeof(XenoShieldSystem)]);
        SubscribeLocalEvent<CrusherShieldComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        SubscribeLocalEvent<CrusherShieldComponent, RemovedShieldEvent>(OnShieldRemove);
        SubscribeLocalEvent<CrusherShieldComponent, XenoDefensiveShieldActionEvent>(OnXenoDefensiveShieldAction);
    }

    private void OnXenoDefensiveShieldAction(Entity<CrusherShieldComponent> xeno, ref XenoDefensiveShieldActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_xenoPlasma.TryRemovePlasma(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        EnsureComp<XenoShieldComponent>(xeno);
        _shield.ApplyShield(xeno, XenoShieldSystem.ShieldType.Crusher, xeno.Comp.Amount);
        ApplyEffects(xeno);

        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("rmc-xeno-defensive-shield-activate", ("user", xeno)), xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-defensive-shield-activate-self", ("user", xeno)), xeno, xeno, PopupType.Medium);
        SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoDefensiveShieldActionEvent>(xeno))
        {
            _actions.SetToggled((action, action), true);
        }
    }


    public void ApplyEffects(Entity<CrusherShieldComponent> ent)
    {
        if (!TryComp<CMArmorComponent>(ent, out var armor))
            return;

        ent.Comp.ExplosionOffAt = _timing.CurTime + ent.Comp.ExplosionResistanceDuration;
        ent.Comp.ShieldOffAt = _timing.CurTime + ent.Comp.ShieldDuration;
        ent.Comp.ExplosionResistApplying = true;

    }

    public void OnShieldRemove(Entity<CrusherShieldComponent> ent, ref RemovedShieldEvent args)
    {
        if (args.Type == XenoShieldSystem.ShieldType.Crusher)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-defensive-shield-end"), ent, ent, PopupType.MediumCaution);
            foreach (var action in _rmcActions.GetActionsWithEvent<XenoDefensiveShieldActionEvent>(ent))
            {
                _actions.SetToggled(action.Owner, false);
            }
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var crusherQuery = EntityQueryEnumerator<CrusherShieldComponent, XenoShieldComponent>();
        while (crusherQuery.MoveNext(out var uid, out var crushShield, out var shield))
        {
            if (crushShield.ExplosionResistApplying && crushShield.ExplosionOffAt <= time)
            {
                crushShield.ExplosionResistApplying = false;
                _popup.PopupEntity(Loc.GetString("rmc-xeno-defensive-shield-resist-end"), uid, uid, PopupType.SmallCaution);
            }

            if (shield.Active && shield.Shield == XenoShieldSystem.ShieldType.Crusher && crushShield.ShieldOffAt <= time)
                _shield.RemoveShield(uid, XenoShieldSystem.ShieldType.Crusher);
        }
    }

    public void OnDamage(Entity<CrusherShieldComponent> ent, ref DamageModifyAfterResistEvent args)
    {
        if (!TryComp<XenoShieldComponent>(ent, out var shield))
            return;

        if (!shield.Active || shield.Shield != XenoShieldSystem.ShieldType.Crusher)
            return;

        foreach (var type in args.Damage.DamageDict)
        {
            if (args.Damage.DamageDict[type.Key] <= 0)
                continue;

            args.Damage.DamageDict[type.Key] -= ent.Comp.DamageReduction;

            if (args.Damage.DamageDict[type.Key] < 0)
                args.Damage.DamageDict[type.Key] = 0;
        }
    }

    public void OnGetExplosionResistance(Entity<CrusherShieldComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (!ent.Comp.ExplosionResistApplying)
            return;

        var explosionResist = ent.Comp.ExplosionResistance;

        var resist = (float) Math.Pow(1.1, explosionResist / 5.0); // From armor calcualtion
        args.DamageCoefficient /= resist;
    }
}
