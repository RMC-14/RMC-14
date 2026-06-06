using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Fluids;
using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;

public sealed class SharedDeployedTrapsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggeredOnEvent>(OnStepTriggered);
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
    }

    private void OnStepTriggered(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggeredOnEvent args)
    {
        _slow.TryRoot(args.Tripper, trap.Comp.StunDuration, true);

        if (!trap.Comp.SoundPlayed)
        {
            _audio.PlayEntity(trap.Comp.CatchSound, Filter.Local(), args.Tripper, false);
        }
        trap.Comp.SoundPlayed = true;

        var caught = EnsureComp<XenoCaughtInTrapComponent>(args.Tripper);
        caught.ExpireTime = _timing.CurTime + trap.Comp.StunDuration;
        caught.Applier = trap.Comp.PlacedBy;

        PredictedQueueDel(trap.Owner);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoCaughtInTrapComponent>();
        while (query.MoveNext(out var uid, out var caught))
        {
            if (_timing.CurTime >= caught.ExpireTime)
                RemCompDeferred<XenoCaughtInTrapComponent>(uid);
        }
    }

    private void OnStepTriggerAttempt(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggerAttemptEvent args)
    {
        args.Continue = !_hive.FromSameHive(args.Tripper, trap.Owner)
                        && !_mobState.IsDead(args.Tripper)
                        && (HasComp<XenoComponent>(args.Tripper) || HasComp<MarineComponent>(args.Tripper));
    }
}
