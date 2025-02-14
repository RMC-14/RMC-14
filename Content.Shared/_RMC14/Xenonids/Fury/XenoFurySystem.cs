using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Tantrum;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Fury;

public sealed partial class XenoFurySystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private readonly HashSet<Entity<XenoComponent>> _xenos = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoFuryComponent, MeleeHitEvent>(OnFuryHit);
    }

    private void OnFuryHit(Entity<XenoFuryComponent> xeno, ref MeleeHitEvent args)
    {
        if (!_xeno.CanHeal(xeno))
            return;

        var validHit = false;

        foreach (var ent in args.HitEntities)
        {
            if (_xeno.CanAbilityAttackTarget(xeno, ent))
            {
                validHit = true;
                break;
            }
        }

        if (!validHit)
            return;

        var healAmount = HasComp<TantrumingComponent>(xeno) ? xeno.Comp.BoostedHeal : xeno.Comp.Heal;

        _xenos.Clear();
        _entityLookup.GetEntitiesInRange(xeno.Owner.ToCoordinates(), xeno.Comp.Range, _xenos);

        foreach (var otherXeno in _xenos)
        {
            if (!_xeno.CanHeal(otherXeno))
                continue;

            if (_mob.IsDead(otherXeno))
                continue;

            if (!_hive.FromSameHive(xeno.Owner, otherXeno.Owner))
                continue;

            var toHeal = -_rmcDamageable.DistributeTypesTotal(otherXeno.Owner, healAmount);
            _damageable.TryChangeDamage(otherXeno.Owner, toHeal);

            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.Effect, otherXeno.Owner.ToCoordinates());
        }
    }
}
