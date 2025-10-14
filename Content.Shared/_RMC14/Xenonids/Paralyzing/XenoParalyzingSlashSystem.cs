using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared.Actions;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Paralyzing;

public sealed class XenoParalyzingSlashSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly RMCDazedSystem _daze = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoParalyzingSlashComponent, XenoParalyzingSlashActionEvent>(OnXenoParalyzingSlashAction);
        SubscribeLocalEvent<XenoActiveParalyzingSlashComponent, MeleeHitEvent>(OnXenoParalyzingSlashHit);
        SubscribeLocalEvent<XenoActiveParalyzingSlashComponent, ComponentShutdown>(OnXenoParalyzingSlashRemoved);
    }

    private void OnXenoParalyzingSlashAction(Entity<XenoParalyzingSlashComponent> xeno, ref XenoParalyzingSlashActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;
        var active = EnsureComp<XenoActiveParalyzingSlashComponent>(xeno);

        active.ExpireAt = _timing.CurTime + xeno.Comp.ActiveDuration;
        active.ParalyzeDelay = xeno.Comp.StunDelay;
        active.ParalyzeDuration = xeno.Comp.StunDuration;
        active.DazeTime = xeno.Comp.DazeTime;

        Dirty(xeno, active);

        _popup.PopupClient(Loc.GetString("cm-xeno-paralyzing-slash-activate"), xeno, xeno);
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoParalyzingSlashActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), true);
        }
    }

    private void OnXenoParalyzingSlashRemoved(Entity<XenoActiveParalyzingSlashComponent> xeno, ref ComponentShutdown args)
    {
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoParalyzingSlashActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), false);
        }
    }

    private void OnXenoParalyzingSlashHit(Entity<XenoActiveParalyzingSlashComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity) ||
                HasComp<VictimBeingParalyzedComponent>(entity) ||
                HasComp<XenoComponent>(entity))
            {
                continue;
            }

            if (HasComp<SynthComponent>(entity))
            {
                var immuneMsg = Loc.GetString("cm-xeno-paralyzing-slash-immune", ("target", entity));
                _popup.PopupEntity(immuneMsg, entity, entity, PopupType.SmallCaution);
                continue;
            }

            _daze.TryDaze(entity, xeno.Comp.DazeTime, true, stutter: true);
            _jitter.DoJitter(entity, xeno.Comp.ParalyzeDelay, true);

            if (!HasComp<XenoComponent>(entity))
            {
                var victim = EnsureComp<VictimBeingParalyzedComponent>(entity);

                victim.ParalyzeAt = _timing.CurTime + xeno.Comp.ParalyzeDelay;
                victim.ParalyzeDuration = xeno.Comp.ParalyzeDuration;

                Dirty(entity, victim);
            }

            var message = Loc.GetString("cm-xeno-paralyzing-slash-hit", ("target", entity));

            if (_net.IsServer)
                _popup.PopupEntity(message, entity, xeno);

            RemCompDeferred<XenoActiveParalyzingSlashComponent>(xeno);
            break;
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        if (_net.IsServer)
        {
            var activeQuery = EntityQueryEnumerator<XenoActiveParalyzingSlashComponent>();
            while (activeQuery.MoveNext(out var uid, out var active))
            {
                if (active.ExpireAt > time)
                    continue;

                RemCompDeferred<XenoActiveParalyzingSlashComponent>(uid);

                _popup.PopupEntity(Loc.GetString("cm-xeno-paralyzing-slash-expire"), uid, uid, PopupType.SmallCaution);
            }
        }

        var victimQuery = EntityQueryEnumerator<VictimBeingParalyzedComponent>();
        while (victimQuery.MoveNext(out var uid, out var victim))
        {
            if (victim.ParalyzeAt > time)
                continue;

            RemCompDeferred<VictimBeingParalyzedComponent>(uid);
            _stun.TryParalyze(uid, victim.ParalyzeDuration, true);
        }
    }
}
