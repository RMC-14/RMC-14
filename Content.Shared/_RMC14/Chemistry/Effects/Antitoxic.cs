using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class Antitoxic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";

    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";

    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "StatusEffectSeeingRainbow";
    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond * 2;
        return $"Heals [color=green]{healing}[/color] toxin damage and removes [color=green]1.25[/color] units of toxic chemicals from the bloodstream.\n" +
               $"Overdoses cause [color=red]{ActualPotency}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{ActualPotency}[/color] brute and [color=red]{ActualPotency}[/color] burn damage, [color=red]{ActualPotency * 3}[/color] toxin damage, and cause [color=red]5[/color] seconds of unconsciousness with a [color=red]5%[/color] chance";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var cmDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = cmDamageable.DistributeHealing(args.TargetEntity, ToxinGroup, potency * 2f);

        // TODO RMC14 remove genetic heal once other meds are in for genetic damage
        healing = cmDamageable.DistributeHealing(args.TargetEntity, GeneticGroup, potency * 2f, healing);
        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);

        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        bloodstream.RemoveBloodstreamToxins(args.TargetEntity, 1.25f);

        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryRemoveTime(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(5));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 eye damage
        var damage = new DamageSpecifier();
        damage.DamageDict[ToxinGroup] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[HeatType] = potency;
        damage.DamageDict[ToxinGroup] = potency * 3;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f))
            return;

        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCUnconsciousComponent>(
            args.TargetEntity,
            Unconscious,
            TimeSpan.FromSeconds(5),
            true
        );
    }
}
