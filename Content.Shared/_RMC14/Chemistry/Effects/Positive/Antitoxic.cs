using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Antitoxic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";

    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond * 2;
        return $"Heals [color=green]{healing}[/color] toxin damage and removes [color=green]0.125[/color] units of toxic chemicals from the bloodstream.\n" +
               //$"Overdoses cause [color=red]{PotencyPerSecond}[/color] damage to the eyes.\n" +
               $"Critical overdoses cause [color=red]5[/color] seconds of unconsciousness with a [color=red]5%[/color] chance";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, ToxinGroup, potency * 2f);

        // TODO RMC14 remove genetic heal once other meds are in for genetic damage
        healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, GeneticGroup, potency * 2f, healing);
        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);

        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        bloodstream.RemoveBloodstreamToxins(args.TargetEntity, 0.125f);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 eye damage
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f))
            return;

        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCUnconsciousComponent>(
            args.TargetEntity,
            Unconscious,
            TimeSpan.FromSeconds(5),
            false
        );
    }
}
