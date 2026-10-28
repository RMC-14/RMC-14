using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Sentry;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Repairing : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private const float StructureHealMultiplier = 6f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Repairs inorganic materials such as barricades and synthetics. Also heals barricades and defenses when sprayed on them.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HealStructure(damageable, args);
            return;
        }

        // TODO RMC14: pick random robot limb & heal it for MEDIUM(2)*potency - 2 * potency brute & 2 * potency
        // burn per 2s tick on one random limb, only if that limb is robotic/synthskin
    }

    private void HealStructure(DamageableSystem damageable, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        if (!entities.HasComponent<BarricadeComponent>(args.TargetEntity) &&
            !entities.HasComponent<SentryComponent>(args.TargetEntity) &&
            !entities.HasComponent<TurretComponent>(args.TargetEntity))
        {
            return;
        }

        var heal = FixedPoint2.New(ActualPotency * StructureHealMultiplier);
        if (heal <= FixedPoint2.Zero)
            return;

        var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
        var specifier = rmcDamageable.DistributeHealing(args.TargetEntity, BruteGroup, heal);
        var leftover = heal + specifier.GetTotal();
        if (leftover > FixedPoint2.Zero)
            rmcDamageable.DistributeHealingCached(args.TargetEntity, BurnGroup, leftover, specifier);

        damageable.TryChangeDamage(args.TargetEntity, specifier, true, interruptsDoAfters: false);
    }

    // TODO RMC14: heal every robotic/synthskin limb on touch for potency*volume brute+burn

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 5f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
