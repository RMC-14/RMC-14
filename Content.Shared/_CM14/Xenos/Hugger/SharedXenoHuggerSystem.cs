using Content.Shared._CM14.Xenos.Leap;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Hugger;

public abstract class SharedXenoHuggerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoHuggerComponent, XenoLeapHitEvent>(OnHuggerLeapHit);

        SubscribeLocalEvent<HuggerSpentComponent, MapInitEvent>(OnHuggerSpentMapInit);
        SubscribeLocalEvent<HuggerSpentComponent, UpdateMobStateEvent>(OnHuggerSpentUpdateMobState);

        SubscribeLocalEvent<VictimHuggedComponent, MapInitEvent>(OnVictimHuggedMapInit);
        SubscribeLocalEvent<VictimHuggedComponent, EntityUnpausedEvent>(OnVictimHuggedUnpaused);
        SubscribeLocalEvent<VictimHuggedComponent, ComponentRemove>(OnVictimHuggedRemoved);
        SubscribeLocalEvent<VictimHuggedComponent, CanSeeAttemptEvent>(OnVictimHuggedCancel);

        SubscribeLocalEvent<VictimBurstComponent, MapInitEvent>(OnVictimBurstMapInit);
        SubscribeLocalEvent<VictimBurstComponent, UpdateMobStateEvent>(OnVictimUpdateMobState);
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

        _blindable.UpdateIsBlind(args.Hit);
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
            }

            if (_net.IsClient)
                continue;

            if (hugged.BurstAt > time)
                continue;

            RemCompDeferred<VictimHuggedComponent>(uid);
            Spawn(hugged.BurstSpawn, xform.Coordinates);
            EnsureComp<VictimBurstComponent>(uid);

            _audio.PlayPvs(hugged.BurstSound, uid);
        }
    }
}
