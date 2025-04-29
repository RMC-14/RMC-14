using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.CriticalGrace;

public sealed partial class CriticalGraceSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CriticalGraceTimeComponent, UpdateMobStateEvent>(OnCriticalGraceMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);
        SubscribeLocalEvent<InCriticalGraceComponent, ComponentShutdown>(OnInCriticalGraceRemove);
    }

    private void OnCriticalGraceMobState(Entity<CriticalGraceTimeComponent> ent, ref UpdateMobStateEvent args)
    {
        if (args.State != Shared.Mobs.MobState.Critical || !_mobState.HasState(ent, Shared.Mobs.MobState.Critical))
            return;

        if (TryComp<InCriticalGraceComponent>(ent, out var crit))
        {
            if (!crit.Over)
                args.State = Shared.Mobs.MobState.Alive;
            return;
        }

        //If already crit/dead can't crit gracee
        if (!_mobState.IsAlive(ent))
            return;

        var grace = EnsureComp<InCriticalGraceComponent>(ent);
        var ev = new GetCriticalGraceTimeEvent(ent.Comp.GraceDuration);
        RaiseLocalEvent(ent, ref ev);
        grace.GraceEndsAt = _timing.CurTime + ev.Time;

        args.State = Shared.Mobs.MobState.Alive;
    }

    private void OnInCriticalGraceRemove(Entity<InCriticalGraceComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            _mobThresholds.VerifyThresholds(ent, thresholds);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var graceQuery = EntityQueryEnumerator<InCriticalGraceComponent>();

        while (graceQuery.MoveNext(out var uid, out var grace))
        {
            if (time < grace.GraceEndsAt)
                continue;

            grace.Over = true;
            RemCompDeferred<InCriticalGraceComponent>(uid);
            Dirty(uid, grace);
        }
    }
}
