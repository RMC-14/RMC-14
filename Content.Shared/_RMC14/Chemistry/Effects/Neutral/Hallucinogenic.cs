using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Jittering;
using Content.Shared._RMC14.Stun;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hallucinogenic : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";
    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";

    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "RMCTwitch",
        "RMCDrool",
        "RMCMoan",
        "RMCGiggle",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes perception-like experiences without an external stimulus, such as hallucinations.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();

        ApplyCapped(status, timing, args.TargetEntity, Hallucination, (float) potency, ActualPotency * 10f);

        if (ActualPotency > 2)
            args.EntityManager.System<RMCJitterinessSystem>().MakeJittery(args.TargetEntity, 2.5f);

        ApplyCapped(status, timing, args.TargetEntity, SeeingRainbows, (float) potency * 2f, ActualPotency * 20f);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, forceEmote: true);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: random step. Tried to make this work but it just felt really awful

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, Hallucination, TimeSpan.FromSeconds(5));

        args.EntityManager.System<RMCJitterinessSystem>().MakeJittery(args.TargetEntity, 2.5f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: brain damage - cm13: 0.5 * potency * delta_time (potency per 2s tick)

        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40));
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
}
