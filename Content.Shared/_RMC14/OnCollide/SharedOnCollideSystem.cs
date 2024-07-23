using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.OnCollide;

public abstract class SharedOnCollideSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<CollideChainComponent> _collideChainQuery;
    private EntityQuery<DamageOnCollideComponent> _damageOnCollideQuery;

    public override void Initialize()
    {
        _collideChainQuery = GetEntityQuery<CollideChainComponent>();
        _damageOnCollideQuery = GetEntityQuery<DamageOnCollideComponent>();

        SubscribeLocalEvent<DamageOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<DamageOnCollideComponent> ent, ref StartCollideEvent args)
    {
        OnCollide(ent, args.OtherEntity);
    }

    private void OnCollide(Entity<DamageOnCollideComponent> ent, EntityUid other)
    {
        if (ent.Comp.Damaged.Contains(other))
            return;

        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, other))
            return;

        if (!ent.Comp.DamageDead && _mobState.IsDead(other))
            return;

        ent.Comp.Damaged.Add(other);
        Dirty(ent);

        if (ent.Comp.Chain == null || AddToChain(ent.Comp.Chain.Value, other))
        {
            _damageable.TryChangeDamage(other, ent.Comp.ChainDamage);
            DoNewCollide(ent, other);
        }
        else
        {
            _damageable.TryChangeDamage(other, ent.Comp.Damage);
        }
    }

    protected virtual void DoNewCollide(Entity<DamageOnCollideComponent> ent, EntityUid other)
    {
    }

    private bool AddToChain(Entity<CollideChainComponent?> chain, EntityUid add)
    {
        if (!_collideChainQuery.Resolve(chain, ref chain.Comp, false))
            return true;

        if (chain.Comp.Hit.Add(add))
        {
            Dirty(chain);
            return true;
        }

        return false;
    }

    public Entity<CollideChainComponent> SpawnChain()
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        var comp = EnsureComp<CollideChainComponent>(ent);
        return (ent, comp);
    }

    public void SetChain(Entity<DamageOnCollideComponent?> ent, EntityUid chain)
    {
        if (!_damageOnCollideQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Chain = chain;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DamageOnCollideComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.InitDamaged)
                continue;

            comp.InitDamaged = true;

            foreach (var contact in _physics.GetEntitiesIntersectingBody(uid, (int) comp.Collision))
            {
                OnCollide((uid, comp), contact);
            }
        }
    }
}
