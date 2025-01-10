using Content.Shared._RMC14.Xenonids.Finesse;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.TailTrip;

public sealed class XenoTailTripSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTailTripComponent, XenoTailTripActionEvent>(OnXenoTailTripAction);

        SubscribeLocalEvent<TailTripSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveTailTrip);
        SubscribeLocalEvent<TailTripSlowedComponent, ComponentRemove>(OnTailTripSlowRemoved);
    }

    private void OnXenoTailTripAction(Entity<XenoTailTripComponent> xeno, ref XenoTailTripActionEvent args)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, args.Target))
            return;

        if (args.Handled)
            return;

        if (!_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.TailEffect, args.Target.ToCoordinates());

        EnsureComp<XenoSweepingComponent>(xeno);
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        if (HasComp<XenoMarkedComponent>(args.Target))
        {
            _stun.TryParalyze(args.Target, xeno.Comp.MarkedStunTime, true);
            // TODO RMC14 welder vision
            _stutter.DoStutter(args.Target, xeno.Comp.MarkedDazeTime, true);
            RemCompDeferred<XenoMarkedComponent>(args.Target);
        }
        else
        {
            _stun.TryParalyze(args.Target, xeno.Comp.StunTime, true);
            EnsureComp<TailTripSlowedComponent>(args.Target).ExpiresAt = _timing.CurTime + xeno.Comp.SlowTime;
            _speed.RefreshMovementSpeedModifiers(args.Target);
            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.SlowEffect, args.Target.ToCoordinates());
        }
    }

    private void OnRefreshMoveTailTrip(Entity<TailTripSlowedComponent> victim, ref RefreshMovementSpeedModifiersEvent args)
    {
        var mod = victim.Comp.SpeedMult.Float();
        args.ModifySpeed(mod, mod);
    }

    private void OnTailTripSlowRemoved(Entity<TailTripSlowedComponent> victim, ref ComponentRemove args)
    {
        if(!TerminatingOrDeleted(victim))
            _speed.RefreshMovementSpeedModifiers(victim);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var tailSlowed = EntityQueryEnumerator<TailTripSlowedComponent>();

        while (tailSlowed.MoveNext(out var uid, out var slowed))
        {
            if (slowed.ExpiresAt > time)
                continue;

            RemCompDeferred<TailTripSlowedComponent>(uid);
        }
    }
}
