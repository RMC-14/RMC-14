using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Psychostimulating : RMCChemicalEffect
{
    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";
    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";
    private static readonly TimeSpan ReductionDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HallucinationCap = TimeSpan.FromSeconds(200);

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stimulates psychological functions, causing increased awareness, focus, and anti-depressing effects.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Reagent is not { } reagent || args.Source is not { } source)
            return;

        var entities = args.EntityManager;
        var target = args.TargetEntity;
        var timing = IoCManager.Resolve<IGameTiming>();
        var popup = entities.System<SharedPopupSystem>();
        var throttle = entities.EnsureComponent<PsychostimulatingThrottleComponent>(target);

        // cm13 compares potency with ==, so only exactly 1, 2 & 3 get partial tiers
        var tier = ActualPotency switch
        {
            1f => "1",
            2f => "2",
            3f => "3",
            _ => "max",
        };

        if (source.GetTotalPrototypeQuantity(reagent.ID) <= FixedPoint2.New(0.1))
        {
            if (throttle.NextReduction.Remove(reagent.ID))
                popup.PopupEntity(Loc.GetString($"chem-psychostimulating-unstable-{tier}"), target, target, PopupType.SmallCaution);

            return;
        }

        if (throttle.NextReduction.TryGetValue(reagent.ID, out var next) && timing.CurTime <= next)
            return;

        throttle.NextReduction[reagent.ID] = timing.CurTime + ReductionDelay;
        popup.PopupEntity(Loc.GetString($"chem-psychostimulating-stable-{tier}"), target, target);

        // cm13 confused decays 1 per 2s tick, so 1 unit = 2s
        var status = entities.System<SharedStatusEffectsSystem>();
        if (tier == "max")
            status.TryRemoveStatusEffect(target, ConfusedStatus);
        else
            status.TryAddTime(target, ConfusedStatus, TimeSpan.FromSeconds(-2f * ActualPotency));
    }

    // TODO RMC14: brain damage on overdose - 0.5 * potency * delta_time (potency per 2s tick)

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();
        if (status.TryGetTime(args.TargetEntity, Hallucination, out var time) &&
            time.EndEffectTime is { } end &&
            end - timing.CurTime > HallucinationCap)
        {
            status.TrySetStatusEffectDuration(args.TargetEntity, Hallucination, HallucinationCap);
        }

        // TODO RMC14: brain damage on critical overdose - 5 * potency brain damage per 2s tick
    }
}

[RegisterComponent]
public sealed partial class PsychostimulatingThrottleComponent : Component
{
    [DataField]
    public Dictionary<string, TimeSpan> NextReduction = new();
}
