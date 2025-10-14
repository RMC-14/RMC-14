using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Projectiles.StoppingPower;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Ranged.AimedShot.FocusedShooting;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Projectiles.Aimed;

public sealed class AimedProjectileSystem : EntitySystem
{
    private const float BigXenoSlowDurationMultiplier = 0.6f;
    private const float BigXenoBlindDurationMultiplier = 0.4f;
    private const string BlindKey = "Blinded";

    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AimedProjectileComponent, ProjectileHitEvent>(OnAimedProjectileHit);
        SubscribeLocalEvent<AimedProjectileComponent, BeforeAreaDamageEvent>(OnBeforeAreaDamage);
    }

    /// <summary>
    ///     Apply any bonus effects the projectile has if shot using the aimed shot action.
    /// </summary>
    private void OnAimedProjectileHit(Entity<AimedProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (!TryComp(ent, out AimedShotEffectComponent? aimedEffect) || args.Handled)
            return;

        if (TryComp(ent.Comp.Source, out RMCFocusedShootingComponent? focused))
            CalculateFocusEffects(ent, args.Target, focused, aimedEffect);

        var target = args.Target;
        var superSlowDuration = aimedEffect.SuperSlowDuration;
        var blindDuration = aimedEffect.BlindDuration;

        _sizeStun.TryGetSize(ent, out var size);

        if (target != ent.Comp.Target)
            return;

        // Big xenos have the effect durations reduced
        if (size >= RMCSizes.Big)
        {
            superSlowDuration *= BigXenoSlowDurationMultiplier;
            blindDuration *= BigXenoBlindDurationMultiplier;
        }

        // Apply bonus damage
        var apValue = 0;
        var damage =  args.Damage * aimedEffect.ExtraHits + aimedEffect.CurrentHealthDamage;

        if (TryComp(ent, out CMArmorPiercingComponent? armorPiercing))
            apValue = armorPiercing.Amount;

        _damageable.TryChangeDamage(target, damage, tool: ent.Comp.Source, armorPiercing: apValue);

        // Apply slows
        _slow.TrySlowdown(target, aimedEffect.SlowDuration);
        _slow.TrySuperSlowdown(target, superSlowDuration);

        // Apply blind
        _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(target, BlindKey, blindDuration, false);

        // Apply firestacks
        if (TryComp(ent, out IgniteOnProjectileHitComponent? ignite))
        {
            ignite.Duration += aimedEffect.FireStacksOnHit;
        }
    }

    /// <summary>
    ///     Cancel dealing area damage if an aimed shot hits it's target.
    /// </summary>
    private void OnBeforeAreaDamage(Entity<AimedProjectileComponent> ent, ref BeforeAreaDamageEvent args)
    {
        if (args.Target == ent.Comp.Target)
            args.Cancelled = true;
    }

    /// <summary>
    ///     Applies the focus effects to the projectile based on the amount of focus stacks the shooter has and the type of target.
    /// </summary>
    private void CalculateFocusEffects(Entity<AimedProjectileComponent> ent, EntityUid target, RMCFocusedShootingComponent focusEffect, AimedShotEffectComponent aimedEffect)
    {
        var slowDuration = 0f;
        var focusedFire = false;

        _sizeStun.TryGetSize(target, out var size);

        if (TryComp(ent, out RMCStoppingPowerComponent? stoppingPower))
            slowDuration = stoppingPower.CurrentStoppingPower;

        // Don't slow if the threshold has not been met.
        if (slowDuration < focusEffect.SlowThreshold)
            slowDuration = 0;

        // Small xenos get the full bonus damage and the lowest level of current health damage.
        if (size >= RMCSizes.VerySmallXeno)
        {
            var currentHealthDamage = focusEffect.CurrentHealthDamageSmallXeno;
            var damageIncreaseModifier = focusEffect.BonusDamageXeno;

            // Normal sized xenos get a reduced bonus damage modifier but take more current health damage.
            if (size >= RMCSizes.Xeno)
            {
                currentHealthDamage = focusEffect.CurrentHealthDamageXeno;
                slowDuration = Math.Max(slowDuration - 1, 0);
                focusedFire = true;
            }
            // Big xenos receive the lowest projectile bonus damage, but they take the highest amount of current health damage.
            if (size >= RMCSizes.Big)
            {
                damageIncreaseModifier = focusEffect.BonusDamageBigXeno;
                currentHealthDamage = focusEffect.CurrentHealthDamageBigXeno;
                slowDuration = Math.Max(slowDuration - 1, 0);
                focusedFire = true;
            }

            if (TryComp(target, out DamageableComponent? damageable))
            {
                _mobThresholds.TryGetIncapThreshold(target, out var threshold);
                if(threshold == null)
                    return;

                // Calculate the current health damage
                var damage = new DamageSpecifier();
                damage.DamageDict.Add("Piercing", (threshold.Value - damageable.TotalDamage) * currentHealthDamage);

                // Apply a multiplier to the bonus damage based on the amount of focus stacks.
                if (focusedFire)
                {
                    damage *= focusEffect.BaseFocusMultiplier + focusEffect.FocusMultiplier * focusEffect.FocusCounter;
                    damageIncreaseModifier *= focusEffect.BaseFocusMultiplier + focusEffect.FocusMultiplier * focusEffect.FocusCounter;
                    slowDuration *= focusEffect.BaseFocusMultiplier + focusEffect.FocusMultiplier * focusEffect.FocusCounter;
                }

                aimedEffect.ExtraHits = damageIncreaseModifier;
                aimedEffect.SuperSlowDuration = TimeSpan.FromSeconds(slowDuration);
                aimedEffect.CurrentHealthDamage = damage;
            }
        }

        // Small xenos don't get slowed
        if (size != RMCSizes.SmallXeno && slowDuration > 0)
        {
            aimedEffect.SlowDuration = TimeSpan.FromSeconds(slowDuration);

            // If the slow duration is higher than the threshold, apply a super slow.
            if (slowDuration > focusEffect.SlowThreshold)
                aimedEffect.SuperSlowDuration = TimeSpan.FromSeconds(slowDuration);

            // Apply a short dazed effect if hit by high stopping power.
            if (stoppingPower != null && stoppingPower.CurrentStoppingPower > focusEffect.DazeThreshold)
                _dazed.TryDaze(target, TimeSpan.FromSeconds(focusEffect.DazeDuration));
        }

        Dirty(ent.Owner, aimedEffect);
    }
}
