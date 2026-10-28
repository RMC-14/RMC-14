using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Chemistry.SpeedModifier;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Musclestimulating : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private const float NutritionDrain = 0.5f * 0.05f * 0.5f;

    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "RMCTwitch",
        "RMCShiver",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Stimulates neuromuscular junctions, increasing muscle contraction force and carry weight. High doses may exhaust the cardiac muscles.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        var speedModifier = args.EntityManager.System<RMCReagentSpeedModifierSystem>();
        speedModifier.SetSpeedModifier(args.TargetEntity, $"{args.Reagent?.ID}:{nameof(Musclestimulating)}", -0.25f * ActualPotency);

        args.EntityManager.System<HungerSystem>().ModifyHunger(args.TargetEntity, -NutritionDrain);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f * 2f / 3f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, forceEmote: true);
        }
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            entities.HasComponent<HumanoidAppearanceComponent>(target);
        if (!isXenoHuman)
            return;

        var potency = (float) ActualPotency;
        if (potency <= 0f)
            return;

        var volume = (float) args.Quantity;
        var buildup = entities.System<RMCSpeedBuildupSystem>();
        buildup.AddBuildup(target, volume, 1f / potency, potency * volume, increaseSpeed: true);
    }

    // TODO RMC14: heart organ damage on overdose - 0.5 * potency * delta_time (potency per 2s tick)

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
