using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Ketogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=red]{PotencyPerSecond * 5}[/color] nutrients, causing hunger over time.\n" +
               $"Increases alcohol metabolism rate by [color=green]{PotencyPerSecond}[/color] units.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 5}[/color] nutrition loss, [color=red]{PotencyPerSecond}[/color] toxin damage, and a [color=red]{ActualPotency * 2.5}%[/color] chance of vomiting.\n" +
               $"Critical overdoses will knock you unconscious for [color=red]10[/color] seconds";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        hungerSystem.ModifyHunger(target, -PotencyPerSecond * 5);

        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        var alcoholRemoved = bloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);

        if (!alcoholRemoved)
            return;

        var drunkSystem = args.EntityManager.System<SharedDrunkSystem>();
        drunkSystem.TryApplyDrunkenness(args.TargetEntity, PotencyPerSecond * 5);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        hungerSystem.ModifyHunger(target, -PotencyPerSecond * 5);

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(target, damage, true, interruptsDoAfters: false);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.025f * ActualPotency))
            return;

        var vomitEvent = new RMCVomitEvent(target);
        entityManager.EventBus.RaiseEvent(EventSource.Local, ref vomitEvent);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCUnconsciousComponent>(
            args.TargetEntity,
            Unconscious,
            TimeSpan.FromSeconds(10),
            false
        );
    }
}
