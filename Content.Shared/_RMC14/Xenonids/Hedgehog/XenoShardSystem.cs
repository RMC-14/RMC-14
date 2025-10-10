using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Rejuvenate;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

public sealed class XenoShardSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;
    [Dependency] private readonly XenoShieldSystem _xenoShield = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoShardComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<XenoShardComponent, CMGetArmorEvent>(OnShardGetArmor);
        SubscribeLocalEvent<XenoShardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoShardComponent, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<XenoFireSpikesComponent, ActionXenoFireSpikesEvent>(OnFireSpikes);
        SubscribeLocalEvent<XenoSpikeShedComponent, ActionXenoSpikeShedEvent>(OnSpikeShed);
        SubscribeLocalEvent<XenoSpikeShieldComponent, ActionXenoSpikeShieldEvent>(OnSpikeShield);

        SubscribeLocalEvent<XenoSpikeShieldComponent, RemovedShieldEvent>(OnShieldRemoved);
        SubscribeLocalEvent<XenoShardComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<XenoShardComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Check if cooldown just ended
            if (comp.SpikeShedCooldownEnd <= currentTime && comp.SpikeShedCooldownEnd > TimeSpan.Zero && !comp.SpikeShedCooldownMessageShown)
            {
                comp.SpikeShedCooldownEnd = TimeSpan.Zero;
                comp.SpikeShedCooldownMessageShown = false;
                if (_net.IsServer)
                    _popup.PopupEntity("You feel your ability to gather shards return!", uid, uid);
                Dirty(uid, comp);
            }

            // Only grow shards once per second per entity
            if (comp.Shards < comp.MaxShards && currentTime >= comp.NextShardGrowth)
            {
                comp.NextShardGrowth = currentTime + TimeSpan.FromSeconds(1);
                AddShards((uid, comp), (int)comp.ShardGrowthRate);
            }
        }

        // Handle spike shield expiration
        var shieldQuery = EntityQueryEnumerator<XenoSpikeShieldComponent>();
        while (shieldQuery.MoveNext(out var uid, out var shield))
        {
            if (shield.Active && shield.ShieldExpireAt <= _timing.CurTime)
            {
                shield.Active = false;
                Dirty(uid, shield);
            }
        }
    }

    private void OnDamageChanged(Entity<XenoShardComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= FixedPoint2.Zero)
            return;

        AddShards(ent, ent.Comp.ShardsOnDamage);
    }

    private void OnMeleeHit(Entity<XenoShardComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (_xeno.CanAbilityAttackTarget(ent, target))
            {
                AddShards(ent, ent.Comp.ShardsPerSlash);
                break; // Only gain shards once per attack
            }
        }
    }

    public void AddShards(Entity<XenoShardComponent> ent, int amount)
    {
        // Check if on spike shed cooldown
        if (ent.Comp.SpikeShedCooldownEnd > _timing.CurTime)
            return;

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

        // Check cooldown
        if (ent.Comp.CooldownExpireAt > _timing.CurTime)
            return;

        // Check shard cost with popup
        if (shards.Shards < ent.Comp.ShardCost)
        {
            var needed = ent.Comp.ShardCost - shards.Shards;
            if (_net.IsServer)
                _popup.PopupEntity($"Not enough shards! We need {needed} more!", ent, ent);
            return;
        }

        // Consume shards
        TryConsumeShards((ent, shards), ent.Comp.ShardCost);

        // Set cooldown
        ent.Comp.CooldownExpireAt = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent, ent.Comp);

        // Use exact bone chips implementation with 8 shots and 45-degree deviation
        args.Handled = _xenoProjectile.TryShoot(
            ent,
            args.Target,
            FixedPoint2.Zero,
            ent.Comp.Projectile,
            null,
            ent.Comp.ProjectileCount,
            new Angle(Math.PI / 4), // 45 degrees
            20f,
            target: args.Entity,
            projectileHitLimit: ent.Comp.ProjectileHitLimit
        );
    }

    private void OnSpikeShed(Entity<XenoSpikeShedComponent> ent, ref ActionXenoSpikeShedEvent args)
    {
        if (!TryComp<XenoShardComponent>(ent, out var shards))
            return;

        if (shards.Shards < ent.Comp.MinShards)
            return;

        // Check if on cooldown
        if (shards.SpikeShedCooldownEnd > _timing.CurTime)
            return;

        // Consume all shards
        shards.Shards = 0;

        // Set 30 second cooldown
        shards.SpikeShedCooldownEnd = _timing.CurTime + TimeSpan.FromSeconds(30);
        shards.SpikeShedCooldownMessageShown = true;

        Dirty(ent, shards);
        _armor.UpdateArmorValue(ent.Owner);
        UpdateHedgehogSprite((ent.Owner, shards));

        // Show popup
        if (_net.IsServer)
            _popup.PopupEntity("You have shed your spikes and cannot gain any more for 30 seconds!", ent, ent);

        // Fire projectiles in all directions (40 like CM13 shrapnel_amount)
        _xenoProjectile.TryShoot(
            ent.Owner,
            new EntityCoordinates(ent, Vector2.UnitX * ent.Comp.ShedRadius),
            FixedPoint2.Zero,
            ent.Comp.Projectile,
            null,
            ent.Comp.ProjectileCount,
            new Angle(2 * Math.PI), // Full circle
            20f,
            projectileHitLimit: ent.Comp.ProjectileHitLimit
        );

        // Apply speed boost (remove armor bonus, gain speed)
        if (TryComp<MovementSpeedModifierComponent>(ent, out var movement))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(ent, movement);
        }

        args.Handled = true;
    }

    private void OnSpikeShield(Entity<XenoSpikeShieldComponent> ent, ref ActionXenoSpikeShieldEvent args)
    {
        if (!TryComp<XenoSpikeShieldComponent>(ent, out var shield) || !TryComp<XenoShardComponent>(ent, out var shards))
            return;

        // Check cooldown
        if (shield.CooldownExpireAt > _timing.CurTime)
            return;

        // Check shard cost with popup
        if (shards.Shards < shield.ShardCost)
        {
            var needed = shield.ShardCost - shards.Shards;
            if (_net.IsServer)
                _popup.PopupEntity($"Not enough shards! We need {needed} more!", ent, ent);
            return;
        }

        // Consume shards
        TryConsumeShards((ent, shards), shield.ShardCost);

        // Set cooldown FIRST (11.5 seconds total: 9 + 2.5 buffer)
        shield.CooldownExpireAt = _timing.CurTime + TimeSpan.FromSeconds(11.5);

        // Activate shield
        shield.Active = true;
        shield.ShieldExpireAt = _timing.CurTime + shield.ShieldDuration;

        Dirty(ent, shield);

        // Refresh all systems to ensure proper updates
        _armor.UpdateArmorValue(ent.Owner);
        if (TryComp<MovementSpeedModifierComponent>(ent, out var movement))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(ent, movement);
        }


        // Give overshield with very high HP so it blocks all damage but still triggers spike firing
        _xenoShield.ApplyShield(ent, XenoShieldSystem.ShieldType.Hedgehog, FixedPoint2.New(9999), shield.ShieldDuration);

        // Show CM13-style messages
        var selfMsg = "We ruffle our bone-shard quills, forming a defensive shell!";
        var othersMsg = $"{ent} ruffles its bone-shard quills, forming a defensive shell!";
        _popup.PopupPredicted(selfMsg, othersMsg, ent, ent);
        _aura.GiveAura(ent, Color.Blue, shield.ShieldDuration, 2);

        args.Handled = true;
    }

    private void UpdateHedgehogSprite(Entity<XenoShardComponent> ent)
    {
        // Determine sprite level based on shard count
        // 0-99: level 1, 100-199: level 2, 200-299: level 3, 300: level 4
        var level = ent.Comp.Shards switch
        {
            < 100 => XenoShardLevel.Level1,
            < 200 => XenoShardLevel.Level2,
            < 300 => XenoShardLevel.Level3,
            _ => XenoShardLevel.Level4
        };

        // Use appearance system to update sprite
        _appearance.SetData(ent, XenoShardVisuals.Level, level);
    }

    private void OnMapInit(Entity<XenoShardComponent> ent, ref MapInitEvent args)
    {
        UpdateHedgehogSprite(ent);
    }

    private void OnShieldRemoved(Entity<XenoSpikeShieldComponent> ent, ref RemovedShieldEvent args)
    {
        if (args.Type != XenoShieldSystem.ShieldType.Hedgehog)
            return;

        ent.Comp.Active = false;
        Dirty(ent, ent.Comp);
    }

    private void OnRejuvenate(Entity<XenoShardComponent> ent, ref RejuvenateEvent args)
    {
        // Reset shard cooldown
        ent.Comp.SpikeShedCooldownEnd = TimeSpan.Zero;
        ent.Comp.SpikeShedCooldownMessageShown = false;

        // Set shards to 150
        ent.Comp.Shards = 150;

        // Reset ability cooldowns
        if (TryComp<XenoFireSpikesComponent>(ent, out var fireSpikes))
        {
            fireSpikes.CooldownExpireAt = TimeSpan.Zero;
            Dirty(ent, fireSpikes);
        }

        if (TryComp<XenoSpikeShieldComponent>(ent, out var spikeShield))
        {
            spikeShield.CooldownExpireAt = TimeSpan.Zero;
            Dirty(ent, spikeShield);
        }

        Dirty(ent);
        _armor.UpdateArmorValue(ent.Owner);
        UpdateHedgehogSprite(ent);
    }
}
