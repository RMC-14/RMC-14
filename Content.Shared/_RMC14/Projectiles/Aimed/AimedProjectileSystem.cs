using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
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
        if (!TryComp(ent, out AimedShotEffectComponent? aimedEffect))
            return;

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
        var damage =  args.Damage * aimedEffect.ExtraHits;

        if (TryComp(ent, out CMArmorPiercingComponent? armorPiercing))
            apValue = armorPiercing.Amount;

        _damageable.TryChangeDamage(args.Target, damage, tool: ent.Comp.Source, armorPiercing: apValue);

        // Apply slows
        _slow.TrySlowdown(target, aimedEffect.SlowDuration);
        _slow.TrySuperSlowdown(target, superSlowDuration);

        // Apply blind
        _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(args.Target, BlindKey, blindDuration, false);

        // Apply firestacks
        if (TryComp(target, out FlammableComponent? flammable))
            flammable.FireStacks += aimedEffect.FireStacksOnHit;
    }

    /// <summary>
    ///     Cancel dealing area damage if an aimed shot hits it's target.
    /// </summary>
    private void OnBeforeAreaDamage(Entity<AimedProjectileComponent> ent, ref BeforeAreaDamageEvent args)
    {
        if (args.Target == ent.Comp.Target)
            args.Cancelled = true;
    }
}
