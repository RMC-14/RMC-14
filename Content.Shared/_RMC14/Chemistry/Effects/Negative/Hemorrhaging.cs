using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemorrhaging : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private const float TouchBleedMultiplier = 0.25f;
    private const float HealingReductionDissipation = 0.4f;
    private const float HealingReductionMax = 50f;

    private static readonly ProtoId<EmotePrototype> CoughBloodEmote = "RMCCoughBlood";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes hemorrhaging.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var amount = 0.2f * Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), -amount);
        plantTray.AdjustMutationMod((plant.Owner, plant.Comp), amount);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();

        // TODO RMC14: internal bleeding wound to random limb at prob(VHIGH(5) * potency) per 2s tick skipped on robotic/synthskin limbs
        // new internal bleed drains 0.5 blood per 2s tick until surgery,
        // & a limb that already bleeds internally gets +0.25 instead

        if (random.Prob(MathF.Min(1f, (float) potency * 0.05f)))
        {
            var emoteSystem = args.EntityManager.System<SharedRMCEmoteSystem>();
            emoteSystem.TryEmoteWithChat(
                args.TargetEntity,
                CoughBloodEmote,
                hideLog: true,
                forceEmote: true
            );
            bloodstreamSystem.TryModifyBloodLevel((args.TargetEntity, bloodstream), -10);
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

        var volume = (float) args.Quantity;
        var magnitude = (float) ActualPotency * volume * TouchBleedMultiplier;
        var buildup = entities.System<RMCHealingReductionSystem>();
        buildup.AddBuildup(target, magnitude, HealingReductionDissipation, HealingReductionMax);
    }

    // TODO RMC14: organ damage on overdose - 0.5 * potency damage to a random organ in a random limb per 2s tick

    // TODO RMC14: critical overdose - prob(10 * potency * delta_time) adds an internal_bleeding wound to a random
    // limb (same wound as the normal) & custom_pain("You feel something burst in your [limb]!"), hidden while unconscious, numb or under
    // heavy painkillers (pain reduction 40+, PAIN_REDUCTION_HEAVY, e.g. Painkilling at potency 1+)
}
