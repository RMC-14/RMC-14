using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;

public sealed class SharedDeployedTrapsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggeredOnEvent>(OnStepOnTriggered);
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggeredOffEvent>(OnStepOffTriggered);
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);

        UpdatesBefore.Add(typeof(StepTriggerSystem));
        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnStepOnTriggered(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggeredOnEvent args)
    {
        // Don't trigger trap if it spawned under their feet. They have to specifically walk onto it.
        if (HasComp<XenoNewlyDeployedTrapsComponent>(trap) || trap.Comp.ContactsAtStart.Contains(args.Tripper))
            return;

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

    private void OnStepOffTriggered(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggeredOffEvent args)
    {
        trap.Comp.ContactsAtStart.Remove(args.Tripper);
    }

    private void OnStepTriggerAttempt(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggerAttemptEvent args)
    {
        args.Continue = !_hive.FromSameHive(args.Tripper, trap.Owner)
                        && !_mobState.IsDead(args.Tripper)
                        && (HasComp<XenoComponent>(args.Tripper) || HasComp<MarineComponent>(args.Tripper));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoCaughtInTrapComponent>();
        while (query.MoveNext(out var uid, out var caught))
        {
            if (_timing.CurTime >= caught.ExpireTime)
                RemCompDeferred<XenoCaughtInTrapComponent>(uid);
        }

        var newTrapQuery = EntityQueryEnumerator<XenoNewlyDeployedTrapsComponent, XenoDeployedTrapsComponent>();
        while (newTrapQuery.MoveNext(out var uid, out var newTrap, out var trap))
        {
            if (newTrap.Running)
            {
                trap.ContactsAtStart.UnionWith(_physics.GetContactingEntities(uid));
                RemCompDeferred<XenoNewlyDeployedTrapsComponent>(uid);
            }
        }
    }
}
