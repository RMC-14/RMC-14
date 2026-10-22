using System.Numerics;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Jittering;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hallucinogenic : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "RMCStatusEffectSeeingRainbow";

    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "RMCTwitch",
        "RMCDrool",
        "RMCMoan",
        "RMCGiggle",
    ];

    private static readonly Vector2[] Cardinals =
    [
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes perception-like experiences without an external stimulus, such as hallucinations.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();

        if (ActualPotency > 2)
        {
            ApplyCapped(status, timing, args.TargetEntity, SeeingRainbows, (float) potency, ActualPotency * 10f);

            // cm13: make_jittery(5) per 2s tick, into the jitteriness pool
            args.EntityManager.System<RMCJitterinessSystem>().MakeJittery(args.TargetEntity, 2.5f);
        }

        ApplyCapped(status, timing, args.TargetEntity, SeeingRainbows, (float) potency * 2f, ActualPotency * 20f);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, ignoreActionBlocker: true, forceEmote: true);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f))
            TryStumble(args.EntityManager, args.TargetEntity, random);

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(5));

        // cm13: make_jittery(5) per 2s tick, into the jitteriness pool
        args.EntityManager.System<RMCJitterinessSystem>().MakeJittery(args.TargetEntity, 2.5f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: brain damage

        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40));
    }

    private static void TryStumble(IEntityManager entities, EntityUid target, IRobustRandom random)
    {
        if (entities.GetComponent<TransformComponent>(target).GridUid == null ||
            entities.System<SharedContainerSystem>().IsEntityInContainer(target) ||
            !entities.System<ActionBlockerSystem>().CanMove(target))
        {
            return;
        }

        if (entities.TryGetComponent(target, out CuffableComponent? cuffable) &&
            entities.System<SharedCuffableSystem>().IsCuffed((target, cuffable)))
        {
            return;
        }

        entities.System<RMCPullingSystem>().TryStopPullsOn(target);
        var physics = entities.System<SharedPhysicsSystem>();
        physics.SetLinearVelocity(target, Vector2.Zero);
        physics.SetAngularVelocity(target, 0f);
        entities.System<ThrowingSystem>().TryThrow(target, random.Pick(Cardinals), animated: false, playSound: false, doSpin: false, compensateFriction: true, recoil: false);
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

        if (current >= cap)
            return;

        var next = Math.Min(current + increment, cap);
        if (next <= 0f)
            return;

        status.TrySetStatusEffectDuration(target, proto, TimeSpan.FromSeconds(next));
    }
}
