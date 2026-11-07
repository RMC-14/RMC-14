using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Oculopeutic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Restores eyesight. Forces potency and cosmetic mutations to occur in plants.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity))
            return;

        // TODO RMC14: heal eye organ damage - heals potency eye damage per 2s tick

        var status = args.EntityManager.System<StatusEffectsSystem>();
        var reduction = TimeSpan.FromSeconds((float) potency * 10f);
        status.TryRemoveTime(args.TargetEntity, "Blinded", reduction);

        if (!args.EntityManager.HasComponent<RMCUnconsciousComponent>(args.TargetEntity))
            status.TryRemoveTime(args.TargetEntity, TemporaryBlindnessSystem.BlindingStatusEffect, reduction);
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(args.EntityManager, plant, "Potency", 1f);
        EnableMutationSlot(args.EntityManager, plant, "Bioluminescence", 1f);
        EnableMutationSlot(args.EntityManager, plant, "Flowers", 1f);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: mob effect - brain damage on critical overdose - potency brain damage per 2s tick

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[HeatType] = potency;
        damage.DamageDict[PoisonType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
