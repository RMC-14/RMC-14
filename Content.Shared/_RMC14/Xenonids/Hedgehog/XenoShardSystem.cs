using Content.Shared._RMC14.Armor;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.GameStates;
using Content.Shared.Appearance;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

public sealed class XenoShardSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private float _nextShardGrowth = 0f;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoShardComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<XenoShardComponent, CMGetArmorEvent>(OnShardGetArmor);
        
        SubscribeLocalEvent<XenoFireSpikesComponent, ActionXenoFireSpikesEvent>(OnFireSpikes);
        SubscribeLocalEvent<XenoSpikeShedComponent, ActionXenoSpikeShedEvent>(OnSpikeShed);
        SubscribeLocalEvent<XenoSpikeShieldComponent, ActionXenoSpikeShieldEvent>(OnSpikeShield);
        SubscribeLocalEvent<XenoSpikeShieldComponent, DamageChangedEvent>(OnSpikeShieldDamaged);
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime.TotalSeconds < _nextShardGrowth)
            return;

        _nextShardGrowth = (float)_timing.CurTime.TotalSeconds + 1f;

        var query = EntityQueryEnumerator<XenoShardComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Shards < comp.MaxShards)
            {
                AddShards((uid, comp), (int)comp.ShardGrowthRate);
            }
        }
    }

    private void OnDamageChanged(Entity<XenoShardComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= FixedPoint2.Zero)
            return;

        AddShards(ent, ent.Comp.ShardsOnDamage);
    }

    public void AddShards(Entity<XenoShardComponent> ent, int amount)
    {
        var oldShards = ent.Comp.Shards;
        ent.Comp.Shards = Math.Min(ent.Comp.Shards + amount, ent.Comp.MaxShards);
        
        if (ent.Comp.Shards != oldShards)
        {
            Dirty(ent);
            _armor.UpdateArmorValue(ent.Owner);
            UpdateHedgehogSprite(ent);
        }
    }

    public bool TryConsumeShards(Entity<XenoShardComponent> ent, int amount)
    {
        if (ent.Comp.Shards < amount)
            return false;

        ent.Comp.Shards -= amount;
        Dirty(ent);
        _armor.UpdateArmorValue(ent.Owner);
        UpdateHedgehogSprite((ent.Owner, ent.Comp));
        return true;
    }

    private void OnShardGetArmor(Entity<XenoShardComponent> ent, ref CMGetArmorEvent args)
    {
        if (ent.Comp.ShardsPerArmorBonus <= 0)
            return;
            
        // Bonus armor is 2.5 per 50 shards
        var bonusArmor = (ent.Comp.Shards / ent.Comp.ShardsPerArmorBonus) * ent.Comp.ArmorPerShard;
        args.XenoArmor += (int)bonusArmor;
    }

    private void OnFireSpikes(Entity<XenoFireSpikesComponent> ent, ref ActionXenoFireSpikesEvent args)
    {
        if (!TryComp<XenoShardComponent>(ent, out var shards))
            return;

        if (!TryConsumeShards((ent, shards), ent.Comp.ShardCost))
            return;

        var xform = Transform(ent);
        var worldPos = _transform.GetWorldPosition(xform);
        var direction = (_transform.ToMapCoordinates(args.Target).Position - worldPos).Normalized();
        var angle = direction.ToAngle();
        
        // Fire 8 spikes in a cone
        for (int i = 0; i < ent.Comp.SpikeCount; i++)
        {
            var spread = (i - ent.Comp.SpikeCount / 2f) * 0.2f; // 0.2 radian spread per spike
            var spikeAngle = angle + new Angle(spread);
            var spikeDirection = spikeAngle.ToVec();
            
            // Spawn spike projectile at proper offset from ravager center
            var spawnOffset = spikeDirection * 1.5f; // Spawn further from center
            var spike = Spawn("XenoSpikeProjectile", _transform.ToMapCoordinates(new EntityCoordinates(ent, spawnOffset)));
            if (TryComp<PhysicsComponent>(spike, out var physics))
            {
                _physics.SetLinearVelocity(spike, spikeDirection * 15f, body: physics);
            }
        }
        
        args.Handled = true;
    }

    private void OnSpikeShed(Entity<XenoSpikeShedComponent> ent, ref ActionXenoSpikeShedEvent args)
    {
        if (!TryComp<XenoShardComponent>(ent, out var shards))
            return;

        if (shards.Shards < 50)
            return;

        var spikeCount = shards.Shards / 10; // 1 spike per 10 shards
        
        // Consume all shards
        shards.Shards = 0;
        Dirty(ent, shards);
        _armor.UpdateArmorValue(ent.Owner);
        UpdateHedgehogSprite((ent.Owner, shards));
        
        var xform = Transform(ent);
        
        // Fire spikes in all directions
        for (int i = 0; i < spikeCount; i++)
        {
            var angle = new Angle(2 * Math.PI * i / spikeCount);
            var direction = angle.ToVec();
            
            // Spawn spike projectile at proper offset from ravager center
            var spawnOffset = direction * 1.5f; // Spawn further from center
            var spike = Spawn("XenoSpikeProjectile", _transform.ToMapCoordinates(new EntityCoordinates(ent, spawnOffset)));
            if (TryComp<PhysicsComponent>(spike, out var physics))
            {
                _physics.SetLinearVelocity(spike, direction * 20f, body: physics);
            }
        }
        
        // Apply speed boost (remove armor bonus, gain speed)
        if (TryComp<MovementSpeedModifierComponent>(ent, out var movement))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(ent, movement);
        }
        
        args.Handled = true;
    }

    private void OnSpikeShield(Entity<XenoSpikeShieldComponent> ent, ref ActionXenoSpikeShieldEvent args)
    {
        if (!TryComp<XenoSpikeShieldComponent>(ent, out var shield))
            return;
            
        shield.Active = !shield.Active;
        Dirty(ent, shield);
        
        // Refresh armor when shield state changes
        if (TryComp<CMArmorComponent>(ent, out var armor))
        {
            // Armor will be recalculated through the CMGetArmorEvent
        }
        
        args.Handled = true;
    }

    private void OnSpikeShieldDamaged(Entity<XenoSpikeShieldComponent> ent, ref DamageChangedEvent args)
    {
        if (!ent.Comp.Active || args.DamageDelta == null || args.DamageDelta.GetTotal() <= 0)
            return;

        // Find nearby attackers and damage them
        var xform = Transform(ent);
        var nearbyEntities = _lookup.GetEntitiesInRange(ent, ent.Comp.SpikeRadius);
        
        foreach (var nearby in nearbyEntities)
        {
            if (nearby == ent.Owner)
                continue;
                
            if (!TryComp<DamageableComponent>(nearby, out var damageable))
                continue;
                
            // Apply spike damage to nearby entities
            _damageable.TryChangeDamage(nearby, ent.Comp.SpikeDamage, true);
        }
    }

    private void UpdateHedgehogSprite(Entity<XenoShardComponent> ent)
    {
        // Determine sprite level based on shard count
        // 0-99: level 1, 100-199: level 2, 200-299: level 3, 300: level 4
        var level = ent.Comp.Shards switch
        {
            < 100 => 1,
            < 200 => 2,
            < 300 => 3,
            _ => 4
        };

        // Use appearance system to update sprite
        _appearance.SetData(ent, XenoShardVisuals.Level, level);
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoShardComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<XenoShardComponent> ent, ref MapInitEvent args)
    {
        UpdateHedgehogSprite(ent);
    }
}