using Content.Server.Destructible;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Construction.DeployedTraps;

public sealed class XenoDeployedTrapsSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployedTrapsComponent, DamageChangedEvent>(OnTrapDamaged);
        SubscribeLocalEvent<XenoDeployedTrapsComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
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
        args.Continue = !_hive.FromSameHive(args.Tripper, trap.Owner)
                        && !_mobState.IsDead(args.Tripper)
                        && (HasComp<XenoComponent>(args.Tripper) || HasComp<MarineComponent>(args.Tripper));
    }
}
