using Content.Shared._CM14.Marines;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Hugger;

public abstract class SharedXenoHuggerSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        base.Initialize();

        _marineQuery = GetEntityQuery<MarineComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoLeapComponent, XenoLeapActionEvent>(OnXenoLeapAction);
        SubscribeLocalEvent<XenoLeapComponent, XenoLeapDoAfterEvent>(OnXenoLeapDoAfter);
        SubscribeLocalEvent<XenoLeapComponent, ThrowDoHitEvent>(OnXenoLeapDoHit);

        SubscribeLocalEvent<XenoHuggerComponent, XenoLeapHitEvent>(OnHuggerLeapHit);

        SubscribeLocalEvent<HuggerSpentComponent, MapInitEvent>(OnHuggerSpentMapInit);
        SubscribeLocalEvent<HuggerSpentComponent, UpdateMobStateEvent>(OnHuggerSpentUpdateMobState);

        SubscribeLocalEvent<VictimHuggedComponent, MapInitEvent>(OnVictimHuggedMapInit);
        SubscribeLocalEvent<VictimHuggedComponent, EntityUnpausedEvent>(OnVictimHuggedUnpaused);
        SubscribeLocalEvent<VictimHuggedComponent, ComponentRemove>(OnVictimHuggedRemoved);
        SubscribeLocalEvent<VictimHuggedComponent, CanSeeAttemptEvent>(OnVictimHuggedCancel);
        SubscribeLocalEvent<VictimHuggedComponent, SlipAttemptEvent>(OnVictimHuggedCancel);
        SubscribeLocalEvent<VictimHuggedComponent, StandAttemptEvent>(OnVictimHuggedCancel);
        SubscribeLocalEvent<VictimHuggedComponent, SpeakAttemptEvent>(OnVictimHuggedCancel);
        SubscribeLocalEvent<VictimHuggedComponent, UpdateCanMoveEvent>(OnVictimHuggedCancel);

        SubscribeLocalEvent<VictimBurstComponent, MapInitEvent>(OnVictimBurstMapInit);
        SubscribeLocalEvent<VictimBurstComponent, UpdateMobStateEvent>(OnVictimUpdateMobState);
    }

    private void OnXenoLeapAction(Entity<XenoLeapComponent> xeno, ref XenoLeapActionEvent args)
    {
        args.Handled = true;

        var ev = new XenoLeapDoAfterEvent(GetNetCoordinates(args.Target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            DamageThreshold = FixedPoint2.New(10)
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoLeapDoAfter(Entity<XenoLeapComponent> xeno, ref XenoLeapDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-leap-cancelled"), xeno, xeno);
            return;
        }

        var origin = _transform.GetMapCoordinates(xeno);
        var target = GetCoordinates(args.Coordinates).ToMap(EntityManager, _transform);
        var gomen = target.Position - origin.Position;
        var length = gomen.Length();

        if (length > xeno.Comp.Range)
        {
            gomen *= xeno.Comp.Range.Float() / length;
        }

        _throwing.TryThrow(xeno, gomen, 30, user: xeno, pushbackRatio: 0);
    }

    private void OnXenoLeapDoHit(Entity<XenoLeapComponent> leap, ref ThrowDoHitEvent args)
    {
        var marineId = args.Target;
        if (!_marineQuery.TryGetComponent(marineId, out var marine))
        {
            return;
        }

        if (_physicsQuery.TryGetComponent(leap, out var physics) &&
            _thrownItemQuery.TryGetComponent(leap, out var thrown))
        {
            _thrownItem.LandComponent(leap, thrown, physics, true);
            _thrownItem.StopThrow(leap, thrown);
        }

        var ev = new XenoLeapHitEvent((marineId, marine));
        RaiseLocalEvent(leap, ref ev);
    }

    private void OnHuggerLeapHit(Entity<XenoHuggerComponent> hugger, ref XenoLeapHitEvent args)
    {
        if (HasComp<HuggerSpentComponent>(hugger) ||
            EnsureComp<VictimHuggedComponent>(args.Hit, out var victim))
        {
            return;
        }

        victim.RecoverAt = _timing.CurTime + hugger.Comp.KnockdownTime;

        var container = _container.EnsureContainer<ContainerSlot>(args.Hit, victim.ContainerId);
        _container.Insert(hugger.Owner, container);

        _stun.TryKnockdown(args.Hit, hugger.Comp.KnockdownTime, true);
        _stun.TryStun(args.Hit, hugger.Comp.KnockdownTime, true);
        _blindable.UpdateIsBlind(args.Hit);
        _actionBlocker.UpdateCanMove(args.Hit);

        _appearance.SetData(hugger, victim.HuggedLayer, true);

        EnsureComp<HuggerSpentComponent>(hugger);

        HuggerLeapHit(hugger);
    }

    protected virtual void HuggerLeapHit(Entity<XenoHuggerComponent> hugger)
    {
    }

    private void OnHuggerSpentMapInit(Entity<HuggerSpentComponent> spent, ref MapInitEvent args)
    {
        if (TryComp(spent, out MobStateComponent? mobState))
            _mobState.UpdateMobState(spent, mobState);
    }

    private void OnHuggerSpentUpdateMobState(Entity<HuggerSpentComponent> spent, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnVictimHuggedMapInit(Entity<VictimHuggedComponent> victim, ref MapInitEvent args)
    {
        victim.Comp.FallOffAt = _timing.CurTime + victim.Comp.FallOffDelay;
        victim.Comp.BurstAt = _timing.CurTime + victim.Comp.BurstDelay;

        _appearance.SetData(victim, victim.Comp.HuggedLayer, true);
    }

    private void OnVictimHuggedUnpaused(Entity<VictimHuggedComponent> victim, ref EntityUnpausedEvent args)
    {
        victim.Comp.FallOffAt += args.PausedTime;
        victim.Comp.BurstAt += args.PausedTime;
    }

    private void OnVictimHuggedRemoved(Entity<VictimHuggedComponent> victim, ref ComponentRemove args)
    {
        _blindable.UpdateIsBlind(victim);
        _standing.Stand(victim);
    }

    private void OnVictimHuggedCancel<T>(Entity<VictimHuggedComponent> victim, ref T args) where T : CancellableEntityEventArgs
    {
        if (victim.Comp.LifeStage <= ComponentLifeStage.Running && !victim.Comp.Recovered)
            args.Cancel();
    }

    private void OnVictimBurstMapInit(Entity<VictimBurstComponent> burst, ref MapInitEvent args)
    {
        _appearance.SetData(burst, burst.Comp.BurstLayer, true);

        if (TryComp(burst, out MobStateComponent? mobState))
            _mobState.UpdateMobState(burst, mobState);
    }

    private void OnVictimUpdateMobState(Entity<VictimBurstComponent> burst, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<VictimHuggedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hugged, out var xform))
        {
            if (hugged.FallOffAt < time && !hugged.FellOff)
            {
                hugged.FellOff = true;
                _appearance.SetData(uid, hugged.HuggedLayer, false);
                if (_container.TryGetContainer(uid, hugged.ContainerId, out var container))
                    _container.EmptyContainer(container);
            }

            if (hugged.RecoverAt < time && !hugged.Recovered)
            {
                hugged.Recovered = true;
                _blindable.UpdateIsBlind(uid);
                _actionBlocker.UpdateCanMove(uid);
                _standing.Stand(uid);
            }

            if (hugged.BurstAt > time)
                continue;

            RemCompDeferred<VictimHuggedComponent>(uid);
            Spawn(hugged.BurstSpawn, xform.Coordinates);
            EnsureComp<VictimBurstComponent>(uid);

            _audio.PlayPvs(hugged.BurstSound, uid);
        }
    }
}
