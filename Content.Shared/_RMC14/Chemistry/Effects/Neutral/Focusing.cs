using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Dizziness;
using Content.Shared._RMC14.Jittering;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Focusing : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var focusing = ActualPotency >= 3
            ? ". Also powerful enough to instantly cure mute and blindness."
            : ".";

        return $"Removes [color=green]{PotencyPerSecond}[/color] units of alcoholic substances and [color=green]{PotencyPerSecond * 2}[/color] seconds of drunkenness{focusing}\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 3}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        var drunkSystem = args.EntityManager.System<SharedDrunkSystem>();
        var stutterSystem = args.EntityManager.System<SharedStutteringSystem>();
        var statusEffects = args.EntityManager.System<SharedStatusEffectsSystem>();

        bloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);
        drunkSystem.TryRemoveDrunkenessTime(args.TargetEntity, PotencyPerSecond * 2);
        stutterSystem.DoRemoveStutterTime(args.TargetEntity, (float) potency * 4f);

        args.EntityManager.System<RMCJitterinessSystem>().ReduceJitteriness(args.TargetEntity, (float) potency * 2f);
        args.EntityManager.System<StatusEffectsSystem>().TryRemoveTime(args.TargetEntity, "Jitter", TimeSpan.FromSeconds((float) potency * 4f / 3f));

        statusEffects.TryAddTime(args.TargetEntity, "RMCStatusEffectConfused", TimeSpan.FromSeconds((float) potency * -4f));
        statusEffects.TryAddTime(args.TargetEntity, "StatusEffectDrowsiness", TimeSpan.FromSeconds((float) potency * -2f));

        args.EntityManager.System<RMCDizzinessSystem>().ReduceDizziness(args.TargetEntity, (float) potency * 2f);

        var oldStatus = args.EntityManager.System<StatusEffectsSystem>();
        oldStatus.TryRemoveTime(args.TargetEntity, "Blinded", TimeSpan.FromSeconds((float) potency * 4f));

        if (!(ActualPotency >= 3))
            return;

        oldStatus.TryRemoveStatusEffect(args.TargetEntity, TemporaryBlindnessSystem.BlindingStatusEffect);
        oldStatus.TryRemoveStatusEffect(args.TargetEntity, "Muted");
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
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 3);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
