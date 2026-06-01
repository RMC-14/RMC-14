using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;

public sealed class SharedDeployedTrapsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggeredOnEvent>(OnStepTriggered);
    }

    private void OnStepTriggered(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggeredOnEvent args)
    {
        _slow.TryRoot(args.Tripper, trap.Comp.StunDuration, true);
        _audio.PlayPredicted(trap.Comp.CatchSound, args.Tripper, trap);

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
}
