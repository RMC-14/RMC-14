using Content.Server.Destructible;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Damage;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Construction.DeployedTraps;

public sealed class XenoDeployedTrapsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployedTrapsComponent, DamageChangedEvent>(OnTrapDamaged);
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggeredOnEvent>(OnStepTriggered);
    }

    private void OnTrapDamaged(Entity<XenoDeployedTrapsComponent> trap, ref DamageChangedEvent args)
    {
        if (args.Origin is { } origin && _hive.FromSameHive(origin, trap.Owner))
            return;

        if (args.DamageDelta == null)
            return;

        if (!_destructible.TryGetDestroyedAt(trap.Owner, out var totalHealth))
            return;

        if (args.Damageable.TotalDamage + args.DamageDelta.GetTotal() > totalHealth)
            QueueDel(trap);
    }

    private void OnStepTriggerAttempt(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggerAttemptEvent args)
    {
        args.Continue = !_hive.FromSameHive(args.Tripper, trap.Owner);
    }

    private void OnStepTriggered(Entity<XenoDeployedTrapsComponent> trap, ref StepTriggeredOnEvent args)
    {
        _slow.TryRoot(args.Tripper, trap.Comp.StunDuration, true);
        _audio.PlayPredicted(trap.Comp.CatchSound, args.Tripper, trap);

        var caught = EnsureComp<XenoCaughtInTrapComponent>(args.Tripper);
        caught.ExpireTime = _timing.CurTime + trap.Comp.StunDuration;
        caught.Applier = trap.Comp.PlacedBy;

        QueueDel(trap);
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
