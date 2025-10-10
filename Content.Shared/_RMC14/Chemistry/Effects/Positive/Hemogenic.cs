using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hemogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var baseText = $"Restores [color=green]{PotencyPerSecond}[/color]cl of blood while not hungry.\n" +
                       $"Causes [color=red]{PotencyPerSecond}[/color] nutrient loss per second.\n" +
                       $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
                       $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] additional nutrient loss";

        return ActualPotency > 3
            ? $"Deals [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond * 2}[/color] airloss damage.\n{baseText}"
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

        hungerSystem.ModifyHunger(target, -PotencyPerSecond);

        if (entityManager.TryGetComponent<BloodstreamComponent>(target, out var bloodstream))
        {
            var bloodstreamSystem = entityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevel((target, bloodstream), potency);
        }

        if (!(ActualPotency > 3))// TODO RMC14 Also check if blood volume is over 560 + 10 and if they're a Yautja.
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

        hungerSystem.ModifyHunger(target, -PotencyPerSecond);
    }
}
