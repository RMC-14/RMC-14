using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Lingering;

public sealed class XenoDespoilerLingeringAcidSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<PullableComponent> _pullableQuery;

    public override void Initialize()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _pullableQuery = GetEntityQuery<PullableComponent>();

        SubscribeLocalEvent<XenoDespoilerLingeringAcidComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<XenoDespoilerLingeringAcidComponent, StartCollideEvent>(OnCollide);
    }

    private void OnInit(Entity<XenoDespoilerLingeringAcidComponent> ent, ref MapInitEvent args)
    {
        var min = (float)ent.Comp.MinLifetime.TotalSeconds;
        var max = (float)ent.Comp.MaxLifetime.TotalSeconds;
        var despawn = EnsureComp<TimedDespawnComponent>(ent);
        despawn.Lifetime = _random.NextFloat(min, max);
    }

    private void OnCollide(Entity<XenoDespoilerLingeringAcidComponent> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (!_mobStateQuery.HasComp(target) || _xenoQuery.HasComp(target))
            return;

        if (_pullableQuery.TryComp(target, out var pull) && pull.BeingPulled)
            return;

        var dmg = new DamageSpecifier(ent.Comp.CrossBurnDamage);
        _damageable.TryChangeDamage(target, dmg, ignoreResistances: false, origin: ent.Comp.Caster);
    }
}
