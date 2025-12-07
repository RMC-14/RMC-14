using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Xenonids.Energy;
using Robust.Shared.Audio.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Projectiles;
using Content.Shared._RMC14.Xenonids.Sweep;

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
    [Dependency] private readonly XenoShieldSystem _shield = default!;
    [Dependency] private readonly XenoEnergySystem _energy = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _explosion = default!;

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoShardComponent, DamageChangedEvent>(OnShardHitBy);
        SubscribeLocalEvent<XenoShardComponent, CMGetArmorEvent>(OnShardGetArmor);
        SubscribeLocalEvent<XenoShardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoShardComponent, XenoEnergyChangedEvent>(OnShardLevelChanged);

        SubscribeLocalEvent<XenoSpikeShedComponent, ActionXenoSpikeShedEvent>(OnSpikeShed);

        SubscribeLocalEvent<XenoSpikeShieldComponent, ActionXenoSpikeShieldEvent>(OnSpikeShield);

        SubscribeLocalEvent<XenoShardComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<XenoShardComponent, XenoEnergyGainAttemptEvent>(OnSpikeEnergyGain);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<XenoShardComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Check if cooldown just ended
            if (comp.SpikeShedCooldownEnd != null && currentTime >= comp.SpikeShedCooldownEnd)
            {
                comp.SpikeShedCooldownEnd = null;
                _popup.PopupEntity(Loc.GetString("rmc-shed-spikes-back"), uid, uid, PopupType.Medium);
                Dirty(uid, comp);
            }
        }
    }

    private void OnShardHitBy(Entity<XenoShardComponent> ent, ref DamageChangedEvent args)
    {
        if (args.Damageable.Damage == null || args.Damageable.Damage.GetTotal() <= FixedPoint2.Zero)
            return;

        if (!HasComp<ProjectileComponent>(args.Tool))
            return;

        if (!TryComp<XenoEnergyComponent>(ent, out var energy))
            return;

        _energy.AddEnergy((ent.Owner, energy), ent.Comp.ShardsOnDamage);
    }

    private void OnShardLevelChanged(Entity<XenoShardComponent> ent, ref XenoEnergyChangedEvent args)
    {
        UpdateSpikes(ent, args.NewEnergy);
    }

    private void UpdateSpikes(Entity<XenoShardComponent> ent, FixedPoint2 shards)
    {
        if(TryComp<StunOnExplosionReceivedComponent>(ent, out var explosion))
        {
            if (shards >= 50)
                _explosion.ChangeExplosionStunResistance(ent, explosion, false);
            else
                _explosion.ChangeExplosionStunResistance(ent, explosion, true);

            Dirty(ent);
        }
        _armor.UpdateArmorValue(ent.Owner);
        UpdateHedgehogSprite(ent);
    }

    private void OnShardGetArmor(Entity<XenoShardComponent> ent, ref CMGetArmorEvent args)
    {
        if (ent.Comp.ShardsPerArmorBonus <= 0 || !TryComp<XenoEnergyComponent>(ent, out var shards))
            return;

        // Bonus armor is 2.5 per 50 shards
        var bonusArmor = (shards.Current / ent.Comp.ShardsPerArmorBonus) * ent.Comp.ArmorPerShard;
        args.XenoArmor += (int)bonusArmor;
    }

    private void OnSpikeShed(Entity<XenoSpikeShedComponent> ent, ref ActionXenoSpikeShedEvent args)
    {
        if (!TryComp<XenoShardComponent>(ent, out var shards) || !TryComp<XenoEnergyComponent>(ent, out var energy))
            return;

        if (!_energy.HasEnergyPopup((ent, energy), ent.Comp.MinShards))
            return;

        // Consume all shards
        _energy.RemoveEnergy((ent, energy), energy.Current);

        // Set 30 second cooldown
        shards.SpikeShedCooldownEnd = _timing.CurTime + TimeSpan.FromSeconds(30);

        Dirty(ent, shards);
        _armor.UpdateArmorValue(ent.Owner);
        UpdateHedgehogSprite((ent.Owner, shards));

        // Show popup
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-shed-spikes"), ent, ent);

        _audio.PlayPredicted(ent.Comp.Sound, ent, ent);
        EnsureComp<XenoSweepingComponent>(ent);

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

        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        args.Handled = true;
    }

    private void OnSpikeShield(Entity<XenoSpikeShieldComponent> ent, ref ActionXenoSpikeShieldEvent args)
    {
        if (!TryComp<XenoShardComponent>(ent, out var shards))
            return;

        // Consume shards
        if (!_energy.TryRemoveEnergyPopup(ent.Owner, ent.Comp.ShardCost))
            return;

        ent.Comp.ShieldExpireAt = _timing.CurTime + ent.Comp.ShieldDuration;

        Dirty(ent);

        _shield.ApplyShield(ent, XenoShieldSystem.ShieldType.Hedgehog, ent.Comp.ShieldAmount);

        // Show CM13-style messages
        var selfMsg = Loc.GetString("rmc-spike-shield-self");
        var othersMsg = Loc.GetString("rmc-spike-shield-others", ("user", ent));
        _popup.PopupPredicted(selfMsg, othersMsg, ent, ent);
        _aura.GiveAura(ent, Color.Blue, ent.Comp.ShieldDuration, 2);

        args.Handled = true;
    }

    private void UpdateHedgehogSprite(Entity<XenoShardComponent> ent)
    {
        if (!TryComp<XenoEnergyComponent>(ent, out var energy))
            return;
        // Determine sprite level based on shard count
        // 0-99: level 1, 100-199: level 2, 200-299: level 3, 300: level 4
        var level = energy.Current switch
        {
            < 50 => XenoShardLevel.Level1,
            < 150 => XenoShardLevel.Level2,
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

    private void OnRefreshMovementSpeed(Entity<XenoShardComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // Apply speed boost when on spike shed cooldown (no shards)
        if (ent.Comp.SpikeShedCooldownEnd != null)
        {
            args.ModifySpeed(1.0f + ent.Comp.SpeedModifier, 1.0f + ent.Comp.SpeedModifier);
        }
    }

    private void OnSpikeEnergyGain(Entity<XenoShardComponent> ent, ref XenoEnergyGainAttemptEvent args)
    {
        if (ent.Comp.SpikeShedCooldownEnd != null)
            args.Cancel();
    }
}
