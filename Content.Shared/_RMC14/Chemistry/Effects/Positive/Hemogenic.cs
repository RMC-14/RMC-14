using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared._RMC14.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hemogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var baseText = Loc.GetString("rmc-reagent-effect-guidebook-hemogenic",
            ("potency", PotencyPerSecond.ToString()),
            ("potencyX5", (PotencyPerSecond * 5).ToString()));

        return ActualPotency > 3
            ? $"{Loc.GetString("rmc-reagent-effect-guidebook-hemogenic-strong", ("potency", PotencyPerSecond.ToString()), ("potencyX2", (PotencyPerSecond * 2).ToString()))}\n{baseText}"
            : baseText;
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        if (!entityManager.TryGetComponent<HungerComponent>(target, out var hungerComponent) ||
            hungerSystem.GetHunger(hungerComponent) < 200)
            return;

        hungerSystem.ModifyHunger(target, -PotencyPerSecond); // TODO RMC14 Yuatja get no hunger drain.

        if (entityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream))
        {
            var bloodstreamSystem = entityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevel((target, bloodstream), potency);
        }

        var rmcBloodstreamSystem = entityManager.System<SharedRMCBloodstreamSystem>();
        var shouldApplyDamage = ActualPotency > 3 &&
                                rmcBloodstreamSystem.TryGetBloodSolution(target, out var bloodSolution) &&
                                bloodSolution.Volume > 570; // TODO RMC14 Also check if they're not a Yautja.
        if (!shouldApplyDamage)
            return;
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[AsphyxiationType] = potency * 2;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
        // TODO RMC14 M.reagent_move_delay_modifier += potency
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        hungerSystem.ModifyHunger(target, PotencyPerSecond * -5);
    }
}
