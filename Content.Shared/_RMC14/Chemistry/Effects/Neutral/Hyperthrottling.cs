using Content.Shared._RMC14.Chemistry.ChemEffectFlags;
using Content.Shared._RMC14.Chemistry.SpeedModifier;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hyperthrottling : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";
    private static readonly EntProtoId HyperThrottleStatus = "RMCStatusEffectHyperThrottle";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the brain to operate at several thousand times its normal speed, allowing understanding of all spoken languages.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity) ||
            args.EntityManager.HasComponent<SynthComponent>(args.TargetEntity))
        {
            return;
        }

        var speedModifier = args.EntityManager.System<RMCReagentSpeedModifierSystem>();
        speedModifier.SetSpeedModifier(args.TargetEntity, $"{args.Reagent?.ID}:{nameof(Hyperthrottling)}", 3f * ActualPotency);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();

        var cap = TimeSpan.FromSeconds(20d);
        var remaining = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, SeeingRainbows, out var time) && time.EndEffectTime is { } end)
            remaining = end - timing.CurTime;

        var toAdd = TimeSpan.FromSeconds((double) ActualPotency);
        if (remaining + toAdd > cap)
            toAdd = cap - remaining;

        if (toAdd > TimeSpan.Zero)
            status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, toAdd);

        var newlySet = args.EntityManager.System<RMCChemEffectFlagsSystem>().TrySetFlag(args.TargetEntity, HyperThrottleStatus);
        if (status.TryGetTime(args.TargetEntity, HyperThrottleStatus, out var flag) && flag.EndEffectTime is { } flagEnd)
            args.EntityManager.System<RMCUniversalUnderstandingSystem>().GrantFor(args.TargetEntity, flagEnd - timing.CurTime);

        if (newlySet)
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-hyperthrottling-dream"), args.TargetEntity, args.TargetEntity);
    }

    // TODO RMC14: brain damage on overdose - 1.5 * potency * delta_time (3 * potency per 2s tick)

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(ActualPotency * 4f));
    }
}
