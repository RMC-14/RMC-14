using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Dizziness;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Alcoholic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";
    private static readonly EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";
    private static readonly EntProtoId ForcedSleepingStatus = "StatusEffectForcedSleeping";
    private static readonly ProtoId<StatusEffectPrototype> SlurredStatus = "SlurredSpeech";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Slows brain function response to stimuli, causing intoxication.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        if (status.HasStatusEffect(args.TargetEntity, Neuroshielding.ResistNeuroStatus))
            return;

        var timing = IoCManager.Resolve<IGameTiming>();
        var actualPotency = (float) ActualPotency;

        var dizziness = args.EntityManager.System<RMCDizzinessSystem>();
        dizziness.AddDizzinessCapped(args.TargetEntity, 0.1f * actualPotency, 5f * actualPotency);

        ApplyCapped(status, timing, args.TargetEntity, DrowsinessStatus, 0.5f * actualPotency, 5f * actualPotency);
        ApplyCapped(status, timing, args.TargetEntity, ConfusedStatus, actualPotency, 10f * actualPotency);
        ApplySlurCapped(args.EntityManager, timing, args.TargetEntity, 0.5f * actualPotency, 10f * actualPotency);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency * 0.5f);
        damage.DamageDict[AsphyxiationType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var random = IoCManager.Resolve<IRobustRandom>();
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();
        var actualPotency = (float) ActualPotency;

        var dizziness = args.EntityManager.System<RMCDizzinessSystem>();
        dizziness.AddDizzinessCapped(args.TargetEntity, 0.25f * actualPotency, 7.5f * actualPotency);

        ApplyCapped(status, timing, args.TargetEntity, DrowsinessStatus, actualPotency, 7.5f * actualPotency);

        if (random.Prob(0.02f))
            ApplyCapped(status, timing, args.TargetEntity, ForcedSleepingStatus, 2f * actualPotency, 15f * actualPotency);

        if (random.Prob(actualPotency * 0.02f))
        {
            var vomit = args.EntityManager.System<RMCVomitSystem>();
            vomit.StartVomit(args.TargetEntity);
        }

        ApplyCapped(status, timing, args.TargetEntity, ConfusedStatus, 2f * actualPotency, 15f * actualPotency);
        ApplySlurCapped(args.EntityManager, timing, args.TargetEntity, actualPotency, 15f * actualPotency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damage.DamageDict[AsphyxiationType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var actualPotency = (float) ActualPotency;

        var dizziness = args.EntityManager.System<RMCDizzinessSystem>();
        dizziness.AddDizzinessCapped(args.TargetEntity, 0.5f * actualPotency, 10f * actualPotency);

        ApplyCapped(status, timing, args.TargetEntity, ConfusedStatus, 4f * actualPotency, 20f * actualPotency);
        ApplyCapped(status, timing, args.TargetEntity, DrowsinessStatus, 2f * actualPotency, 10f * actualPotency);
        ApplySlurCapped(args.EntityManager, timing, args.TargetEntity, 2f * actualPotency, 20f * actualPotency);

        var sleepChance = Math.Clamp(5f * actualPotency * 0.01f, 0f, 1f);
        if (random.Prob(sleepChance))
            ApplyCapped(status, timing, args.TargetEntity, ForcedSleepingStatus, 2f * actualPotency, 20f * actualPotency);

        var vomitChance = Math.Clamp(5f * actualPotency * 0.01f, 0f, 1f);
        if (random.Prob(vomitChance))
        {
            var vomit = args.EntityManager.System<RMCVomitSystem>();
            vomit.StartVomit(args.TargetEntity);
        }

        // TODO RMC14: liver damage - 0.5 * potency liver damage per 2s tick
    }

    private static void ApplyCapped(
        SharedStatusEffectsSystem status,
        IGameTiming timing,
        EntityUid target,
        EntProtoId proto,
        float increment,
        float cap)
    {
        if (cap <= 0f)
            return;

        var current = 0f;
        if (status.TryGetTime(target, proto, out var time) && time.EndEffectTime is { } end)
        {
            var remaining = (float) (end - timing.CurTime).TotalSeconds;
            if (remaining > 0f)
                current = remaining;
        }

        var next = Math.Min(current + increment, cap);
        if (next <= 0f)
            return;

        status.TrySetStatusEffectDuration(target, proto, TimeSpan.FromSeconds(next));
    }

    private static void ApplySlurCapped(IEntityManager entMan, IGameTiming timing, EntityUid target, float increment, float cap)
    {
        var current = 0f;
        if (entMan.System<StatusEffectsSystem>().TryGetTime(target, SlurredStatus, out var time))
        {
            var remaining = (float) (time.Value.Item2 - timing.CurTime).TotalSeconds;
            if (remaining > 0f)
                current = remaining;
        }

        var toAdd = Math.Min(current + increment, cap) - current;
        if (toAdd <= 0f)
            return;

        entMan.System<SharedSlurredSystem>().DoSlur(target, TimeSpan.FromSeconds(toAdd));
    }
}
