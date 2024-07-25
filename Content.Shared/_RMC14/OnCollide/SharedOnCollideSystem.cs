using Content.Shared._RMC14.Xenonids.Projectile.Spit;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
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
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly XenoSpitSystem _xenoSpit = default!;

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

        var didEmote = false;
        if (ent.Comp.Chain == null || AddToChain(ent.Comp.Chain.Value, other))
        {
            _damageable.TryChangeDamage(other, ent.Comp.Damage);
            DoEmote(ent, other);
            didEmote = true;
        }
        else
        {
            _damageable.TryChangeDamage(other, ent.Comp.ChainDamage);
        }

        _xenoSpit.SetAcidCombo(other, ent.Comp.AcidComboDuration, ent.Comp.AcidComboDamage, ent.Comp.AcidComboParalyze);

        if (ent.Comp.Paralyze > TimeSpan.Zero)
        {
            _stun.TryParalyze(other, ent.Comp.Paralyze, true);

            if (!didEmote)
                DoEmote(ent, other);
        }
    }

    protected virtual void DoEmote(Entity<DamageOnCollideComponent> ent, EntityUid other)
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
