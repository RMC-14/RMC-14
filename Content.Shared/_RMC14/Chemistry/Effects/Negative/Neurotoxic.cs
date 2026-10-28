using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Jittering;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Neurotoxic : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly EntProtoId ResistNeuroStatus = "RMCStatusEffectResistNeuro";
    private static readonly EntProtoId<SkillDefinitionComponent> EnduranceSkill = "RMCSkillEndurance";
    private static readonly ProtoId<EmotePrototype> DroolEmote = "RMCDrool";

    private const float XenoDazeMax = 30f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the brain. Prevents species mutation from occurring in plants.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var suppress = -Potency;
        SuppressMutationSlot(args.EntityManager, plant, "Mutate Species", suppress);
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        // TODO RMC14: baseline brain damage - 2 * potency brain damage per 2s tick
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var potency = (float) ActualPotency;
        var volume = (float) args.Quantity;
        if (potency <= 0f || volume <= 0f)
            return;

        var isXeno = entities.HasComponent<XenoComponent>(target);
        var isHuman = entities.HasComponent<HumanoidAppearanceComponent>(target);
        if (!isXeno && !isHuman)
            return;

        var dazed = entities.System<RMCDazedSystem>();

        if (isHuman)
        {
            dazed.TryDaze(target, TimeSpan.FromSeconds(potency * volume * 0.5f), true);
            entities.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-neurotoxic-numb"), target, target, PopupType.SmallCaution);
        }

        if (isXeno)
        {
            var remaining = 0f;
            var status = entities.System<SharedStatusEffectsSystem>();
            if (status.TryGetTime(target, RMCDazedSystem.StatusEffectDazed, out var time) && time.EndEffectTime is { } end)
                remaining = (float) (end - IoCManager.Resolve<IGameTiming>().CurTime).TotalSeconds;

            var toAdd = MathF.Min(potency * volume * 0.5f, XenoDazeMax - remaining);
            if (toAdd > 0f)
                dazed.TryDaze(target, TimeSpan.FromSeconds(toAdd), false);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: brain damage on overdose - 3 * potency brain damage per 2s tick

        var cap = TimeSpan.FromSeconds(ActualPotency * 3f);
        var timing = IoCManager.Resolve<IGameTiming>();

        var jitteriness = args.EntityManager.System<RMCJitterinessSystem>();
        var jitterCap = (float) ActualPotency * 3f;
        jitteriness.SetJitteriness(args.TargetEntity, MathF.Min(jitteriness.GetJitteriness(args.TargetEntity) + (float) potency, jitterCap));

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
            args.EntityManager.System<SharedRMCEmoteSystem>().TryEmoteWithChat(args.TargetEntity, DroolEmote, hideLog: true, forceEmote: true);

        if (!random.Prob(0.5f))
            return;

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var drowsyRemaining = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, "StatusEffectDrowsiness", out var drowsyTime) && drowsyTime.EndEffectTime is { } drowsyEnd)
            drowsyRemaining = drowsyEnd - timing.CurTime;

        var drowsyToAdd = TimeSpan.FromSeconds((float) potency);
        if (drowsyRemaining + drowsyToAdd > cap)
            drowsyToAdd = cap - drowsyRemaining;

        if (drowsyToAdd > TimeSpan.Zero)
            status.TryAddStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", drowsyToAdd);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.15f * (float) potency))
            return;

        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<HumanoidAppearanceComponent>(target))
            return;

        var popup = entities.System<SharedPopupSystem>();

        if (entities.System<SkillsSystem>().HasSkill(target, EnduranceSkill, 5))
        {
            popup.PopupEntity(Loc.GetString("chem-neurotoxic-withstands", ("target", target)), target, PopupType.MediumCaution);
            return;
        }

        var status = entities.System<SharedStatusEffectsSystem>();
        if (status.HasStatusEffect(target, ResistNeuroStatus))
        {
            popup.PopupEntity(Loc.GetString("chem-neurotoxic-shrugs-off", ("target", target)), target, PopupType.MediumCaution);
            return;
        }

        var slow = entities.System<RMCSlowSystem>();

        slow.TrySuperSlowdown(target, TimeSpan.FromSeconds(8f));
        popup.PopupEntity(Loc.GetString("chem-neurotoxic-slowed", ("target", target)), target, PopupType.MediumCaution);

        var duration = TimeSpan.FromSeconds(ActualPotency * 4f);

        var inventory = entities.System<InventorySystem>();
        var armored = inventory.TryGetSlotEntity(target, "outerClothing", out var outer) &&
            entities.TryGetComponent<ClothingSpeedModifierComponent>(outer, out var mod) &&
            (mod.WalkModifier != 1f || mod.SprintModifier != 1f);

        if (armored)
            return;

        var oldStatus = entities.System<StatusEffectsSystem>();
        if (oldStatus.TryGetTime(target, "KnockedDown", out var knockdown) &&
            knockdown.Value.Item2 - IoCManager.Resolve<IGameTiming>().CurTime >= TimeSpan.FromSeconds(10))
        {
            return;
        }

        var stun = entities.System<SharedStunSystem>();
        stun.TryKnockdown(target, duration, true);
        stun.TryStun(target, duration, true);
        popup.PopupEntity(Loc.GetString("chem-neurotoxic-falls-prone", ("target", target)), target, PopupType.MediumCaution);
    }
}
