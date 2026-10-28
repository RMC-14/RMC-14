using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Jittering;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Cardiostabilizing : RMCChemicalEffect
{
    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "RMCTwitch",
        "RMCBlink",
        "RMCShiver",
    ];

    public override bool ReactsOnTouch => true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stabilizes the cardiac cycle when under shock.";
    }

    // TODO RMC14: pain reduction, heart organ damage on critical overdose - pain reduction of
    // 20 * potency (reset to 0 when the reagent leaves)
    // 0.25 * delta_time (flat 0.5 heart damage per 2s tick, not potency scaled)

    // TODO RMC14: losebreath = max(10, losebreath - 2.5*potency*delta_time) - losebreath source is
    // bruised lungs (organ_internal.dm)

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
            HandleReaction(args);
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<XenoComponent>(target))
            return;

        if (!entities.TryGetComponent(target, out DamageableComponent? damageableComp))
            return;

        var mobThreshold = entities.System<MobThresholdSystem>();
        if (!mobThreshold.TryGetThresholdForState(target, MobState.Critical, out var critThreshold) ||
            damageableComp.TotalDamage < critThreshold)
            return;

        var volume = (float) args.Quantity;
        var maxHeal = (float) ActualPotency * volume * 0.8f;
        var neededHeal = (float) (damageableComp.TotalDamage - critThreshold) + 1f;
        var healAmount = MathF.Min(maxHeal, neededHeal);
        if (healAmount <= 0f)
            return;

        entities.System<SharedXenoHealSystem>().Heal(target, healAmount);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        args.EntityManager.System<RMCJitterinessSystem>().MakeJittery(args.TargetEntity, 2.5f);

        var stun = args.EntityManager.System<SharedStunSystem>();
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(40), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f))
            return;

        var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
        emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, forceEmote: true);
    }
}
