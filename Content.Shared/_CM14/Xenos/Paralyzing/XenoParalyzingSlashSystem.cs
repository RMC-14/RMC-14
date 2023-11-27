using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Paralyzing;

public sealed class XenoParalyzingSlashSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoParalyzingSlashComponent, XenoParalyzingSlashActionEvent>(OnXenoParalyzingSlashAction);

        SubscribeLocalEvent<XenoActiveParalyzingSlashComponent, EntityUnpausedEvent>(OnXenoParalyzingSlashUnpaused);
        SubscribeLocalEvent<XenoActiveParalyzingSlashComponent, MeleeHitEvent>(OnXenoParalyzingSlashHit);

        SubscribeLocalEvent<VictimBeingParalyzedComponent, EntityUnpausedEvent>(OnVictimBeingParalyzedUnpaused);
    }

    private void OnXenoParalyzingSlashUnpaused(Entity<XenoActiveParalyzingSlashComponent> xeno, ref EntityUnpausedEvent args)
    {
        xeno.Comp.ExpireAt += args.PausedTime;
    }

    private void OnXenoParalyzingSlashAction(Entity<XenoParalyzingSlashComponent> xeno, ref XenoParalyzingSlashActionEvent args)
    {
        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var active = EnsureComp<XenoActiveParalyzingSlashComponent>(xeno);

        active.ExpireAt = _timing.CurTime + xeno.Comp.ActiveDuration;
        active.StunDelay = xeno.Comp.StunDelay;
        active.StunDuration = xeno.Comp.StunDuration;

        Dirty(xeno, active);

        _popup.PopupClient(Loc.GetString("cm-xeno-paralyzing-slash-activate"), xeno, xeno);
    }

    private void OnXenoParalyzingSlashHit(Entity<XenoActiveParalyzingSlashComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanHitLiving(xeno, entity) ||
                HasComp<VictimBeingParalyzedComponent>(entity))
            {
                continue;
            }

            // TODO CM14 slight blindness
            var victim = EnsureComp<VictimBeingParalyzedComponent>(entity);

            victim.ParalyzeAt = _timing.CurTime + xeno.Comp.StunDelay;
            victim.ParalyzeDuration = xeno.Comp.StunDuration;

            Dirty(entity, victim);

            var message = Loc.GetString("cm-xeno-paralyzing-slash-hit", ("target", entity));
            _popup.PopupClient(message, entity, xeno);

            RemCompDeferred<XenoActiveParalyzingSlashComponent>(xeno);
            break;
        }
    }

    private void OnVictimBeingParalyzedUnpaused(Entity<VictimBeingParalyzedComponent> victim, ref EntityUnpausedEvent args)
    {
        victim.Comp.ParalyzeAt += args.PausedTime;
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

                _popup.PopupEntity(Loc.GetString("cm-xeno-paralyzing-slash-expire"), uid, uid);
            }
        }

        var victimQuery = EntityQueryEnumerator<VictimBeingParalyzedComponent>();
        while (victimQuery.MoveNext(out var uid, out var victim))
        {
            if (victim.ParalyzeAt > time)
                continue;

            RemCompDeferred<VictimBeingParalyzedComponent>(uid);
            _stun.TryKnockdown(uid, victim.ParalyzeDuration, true);
            _stun.TryStun(uid, victim.ParalyzeDuration, true);
        }
    }
}
