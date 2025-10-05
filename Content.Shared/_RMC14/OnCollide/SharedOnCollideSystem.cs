using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared._RMC14.Xenonids.Projectile.Spit;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
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
    [Dependency] private readonly ThermalCloakSystem _cloak = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly XenoSpitSystem _xenoSpit = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private EntityQuery<CollideChainComponent> _collideChainQuery;
    private EntityQuery<DamageOnCollideComponent> _damageOnCollideQuery;

    private readonly List<Entity<DamageOnCollideComponent>> _damageOnCollide = new();

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

        if (_hive.FromSameHive(ent.Owner, other))
            return;

        if (ent.Comp.Fire && HasComp<RMCImmuneToFireTileDamageComponent>(other))
            return;

        if (HasComp<UncloakOnHitComponent>(ent.Owner))
            _cloak.TrySetInvisibility(other, false, true);

        ent.Comp.Damaged.Add(other);
        Dirty(ent);

        var didEmote = false;
        if (ent.Comp.Chain == null || AddToChain(ent.Comp.Chain.Value, other))
        {
            var damage = ent.Comp.Damage;
            if (ent.Comp.Acidic)
                damage = _xeno.TryApplyXenoAcidDamageMultiplier(other, damage);
            _damageable.TryChangeDamage(other, damage, ent.Comp.IgnoreResistances);
            DoEmote(ent, other);
            didEmote = true;
        }
        else
        {
            var damage = ent.Comp.ChainDamage;
            if (ent.Comp.Acidic)
                damage = _xeno.TryApplyXenoAcidDamageMultiplier(other, damage);
            _damageable.TryChangeDamage(other, damage, ent.Comp.IgnoreResistances);
        }

        _xenoSpit.SetAcidCombo(other, ent.Comp.AcidComboDuration, ent.Comp.AcidComboDamage, ent.Comp.AcidComboParalyze, ent.Comp.AcidComboResists);

        if (ent.Comp.Paralyze > TimeSpan.Zero && !_standing.IsDown(other) && (!_size.TryGetSize(other, out var size) || size < RMCSizes.Big))
        {
            _stun.TryParalyze(other, ent.Comp.Paralyze, true);

            if (!didEmote)
                DoEmote(ent, other);
        }

        var ev = new DamageCollideEvent(other);
        RaiseLocalEvent(ent, ref ev);
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
        _damageOnCollide.Clear();

        try
        {
            var query = EntityQueryEnumerator<DamageOnCollideComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.InitDamaged)
                    continue;

                comp.InitDamaged = true;
                _damageOnCollide.Add((uid, comp));
            }

            foreach (var entity in _damageOnCollide)
            {
                foreach (var contact in _physics.GetEntitiesIntersectingBody(entity, (int) entity.Comp.Collision))
                {
                    OnCollide(entity, contact);
                }
            }
        }
        finally
        {
            _damageOnCollide.Clear();
        }
    }
}
