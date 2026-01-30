using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Stomp;

public sealed class XenoStompSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoStompComponent, XenoStompActionEvent>(OnXenoStompAction);
        SubscribeLocalEvent<XenoStompComponent, XenoStompDoAfterEvent>(OnXenoStompDoAfter);
    }

    private readonly HashSet<Entity<MobStateComponent>> _receivers = new();

    private void OnXenoStompAction(Entity<XenoStompComponent> xeno, ref XenoStompActionEvent args)
    {
        var attemptEv = new XenoStompAttemptEvent();
        RaiseLocalEvent(xeno, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        if (_mobState.IsDead(xeno))
            return;

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;
        var ev = new XenoStompDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoStompDoAfter(Entity<XenoStompComponent> xeno, ref XenoStompDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (args.Cancelled)
        {
            foreach (var action in _rmcActions.GetActionsWithEvent<XenoStompActionEvent>(xeno))
            {
                _actions.ClearCooldown(action.AsNullable());
            }

            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (!TryComp(xeno, out TransformComponent? xform))
            return;

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.Range, _receivers);

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        foreach (var receiver in _receivers)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (xeno.Comp.SlowBigInsteadOfStun && _size.TryGetSize(receiver, out var size) && size >= RMCSizes.Big)
                _slow.TrySlowdown(receiver, xeno.Comp.DebuffsHurtXenosMore ? _xeno.TryApplyXenoDebuffMultiplier(receiver, xeno.Comp.ParalyzeTime)
                    : xeno.Comp.ParalyzeTime, true);
            else if (!xeno.Comp.ParalyzeUnderOnly)
                _stun.TryParalyze(receiver, xeno.Comp.DebuffsHurtXenosMore ? _xeno.TryApplyXenoDebuffMultiplier(receiver, xeno.Comp.ParalyzeTime)
                    : xeno.Comp.ParalyzeTime, true);

            if (xeno.Comp.Slows)
                _slow.TrySuperSlowdown(receiver, xeno.Comp.SlowTime, true);

            if (xform.Coordinates.TryDistance(EntityManager, receiver.Owner.ToCoordinates(), out var distance) && distance <= xeno.Comp.ShortRange)
            {
                if (!_standing.IsDown(receiver))
                    continue;

                var damage = _damageable.TryChangeDamage(receiver, _xeno.TryApplyXenoSlashDamageMultiplier(receiver, xeno.Comp.Damage), origin: xeno, tool: xeno);
                if (damage?.GetTotal() > FixedPoint2.Zero)
                {
                    var filter = Filter.Pvs(receiver, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                    _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { receiver }, filter);
                }

                if (xeno.Comp.ParalyzeUnderOnly && _size.TryGetSize(receiver, out size) && size < RMCSizes.Big)
                    _stun.TryParalyze(receiver, xeno.Comp.DebuffsHurtXenosMore ? _xeno.TryApplyXenoDebuffMultiplier(receiver, xeno.Comp.ParalyzeTime)
                    : xeno.Comp.ParalyzeTime, true);
            }
        }

        if (_net.IsServer && xeno.Comp.SelfEffect is not null)
            SpawnAttachedTo(xeno.Comp.SelfEffect, xeno.Owner.ToCoordinates());
    }
}
