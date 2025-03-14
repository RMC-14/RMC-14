using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Targeting;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Projectiles;

namespace Content.Shared._RMC14.Projectiles.Aimed;

public sealed class AimedProjectileSystem : EntitySystem
{
    private const float BigXenoSlowDurationMultiplier = 0.6f;
    private const float BigXenoBlindDurationMultiplier = 0.4f;

    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly TargetingSystem _targeting = default!;

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
        var damage =  args.Damage * aimedEffect.ExtraHits;
        _damageable.TryChangeDamage(args.Target, damage);

        // TODO Apply blind

        // Apply slows
        _slow.TrySlowdown(target, aimedEffect.SlowDuration);
        _slow.TrySuperSlowdown(target, superSlowDuration);

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
