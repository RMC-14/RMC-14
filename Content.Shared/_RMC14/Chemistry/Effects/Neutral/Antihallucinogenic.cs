using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Antihallucinogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";
    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";

    private static readonly ProtoId<ReagentPrototype> MindbreakerToxin = "RMCMindbreakerToxin";
    private static readonly ProtoId<ReagentPrototype> SpaceDrugs = "RMCSpaceDrugs";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=green]2.5[/color] units of Mindbreaker Toxin and Space Drugs from the bloodstream. It also stabilizes perceptive abnormalities such as hallucinations\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond}[/color] burn, and [color=red]{PotencyPerSecond * 3}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        bloodstream.RemoveBloodstreamChemical(args.TargetEntity, MindbreakerToxin, 2.5f);
        bloodstream.RemoveBloodstreamChemical(args.TargetEntity, SpaceDrugs, 2.5f);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddTime(args.TargetEntity, Hallucination, TimeSpan.FromSeconds(PotencyPerSecond * -10));
        status.TryAddTime(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(PotencyPerSecond * -20));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency);
        damage = rmcDamageable.DistributeFreshDamage(BurnGroup, potency, damage);
        damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 3, damage);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
