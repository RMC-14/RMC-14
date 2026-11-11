using Content.Shared._RMC14.Dizziness;
using Content.Shared._RMC14.Jittering;
using Content.Shared._RMC14.StatusEffect;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Nervestimulating : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";
    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";
    private static readonly EntProtoId DazedStatus = "Dazed";
    private static readonly EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";
    private static readonly ProtoId<StatusEffectPrototype> BlurStatus = "Blinded";
    private static readonly ProtoId<StatusEffectPrototype> JitterStatus = "Jitter";
    private static readonly ProtoId<StatusEffectPrototype> KnockedDownStatus = "KnockedDown";
    private static readonly ProtoId<StatusEffectPrototype> StunStatus = "Stun";
    private static readonly ProtoId<StatusEffectPrototype> UnconsciousStatus = "Unconscious";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases neuron communication speed, improving reaction time, awareness, and muscular control.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        var stunReduction = TimeSpan.FromSeconds((double) potency * 2);
        var knockdownReduction = TimeSpan.FromSeconds((double) potency * 1.5);

        var oldStatus = args.EntityManager.System<StatusEffectsSystem>();
        oldStatus.TryRemoveTime(args.TargetEntity, StunStatus, ScaleIncapacitation(args, StunStatus, stunReduction));
        oldStatus.TryRemoveTime(args.TargetEntity, KnockedDownStatus, ScaleIncapacitation(args, KnockedDownStatus, knockdownReduction));
        oldStatus.TryRemoveTime(args.TargetEntity, UnconsciousStatus, ScaleIncapacitation(args, UnconsciousStatus, knockdownReduction));

        if (!(ActualPotency > 2))
            return;

        var stutter = args.EntityManager.System<SharedStutteringSystem>();

        var reductionSeconds = 2f * (float) potency;
        var perTickUnitReductionSeconds = 2f * reductionSeconds;
        stutter.DoRemoveStutterTime(args.TargetEntity, perTickUnitReductionSeconds);

        oldStatus.TryRemoveTime(args.TargetEntity, BlurStatus, TimeSpan.FromSeconds(perTickUnitReductionSeconds));

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddTime(args.TargetEntity, ConfusedStatus, TimeSpan.FromSeconds(-perTickUnitReductionSeconds));
        status.TryAddTime(args.TargetEntity, DrowsinessStatus, TimeSpan.FromSeconds(-reductionSeconds));
        args.EntityManager.System<RMCJitterinessSystem>().ReduceJitteriness(args.TargetEntity, reductionSeconds);
        oldStatus.TryRemoveTime(args.TargetEntity, JitterStatus, TimeSpan.FromSeconds(reductionSeconds * 2f / 3f));

        args.EntityManager.System<RMCDizzinessSystem>().ReduceDizziness(args.TargetEntity, reductionSeconds);
    }

    private static TimeSpan ScaleIncapacitation(EntityEffectReagentArgs args, string key, TimeSpan time)
    {
        var ev = new RMCStatusEffectTimeEvent(key, time);
        args.EntityManager.EventBus.RaiseLocalEvent(args.TargetEntity, ref ev);
        return ev.Duration;
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            entities.HasComponent<HumanoidAppearanceComponent>(target);
        if (!isXenoHuman || !(ActualPotency > 3))
            return;

        var oldStatus = entities.System<StatusEffectsSystem>();
        oldStatus.TryRemoveStatusEffect(target, StunStatus);
        oldStatus.TryRemoveStatusEffect(target, KnockedDownStatus);

        var status = entities.System<SharedStatusEffectsSystem>();
        status.TryRemoveStatusEffect(target, DazedStatus);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[HeatType] = potency;
        damage.DamageDict[PoisonType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
