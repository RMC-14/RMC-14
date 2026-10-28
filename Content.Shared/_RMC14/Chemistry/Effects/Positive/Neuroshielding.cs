using Content.Shared._RMC14.Chemistry.ChemEffectFlags;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neuroshielding : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Protects the brain from neurological damage caused by toxins.";
    }

    // TODO RMC14: liver organ damage - 0.5 * potency * delta_time (potency per 2s tick)

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity) ||
            entities.HasComponent<SynthComponent>(args.TargetEntity))
        {
            return;
        }

        if (!entities.System<RMCChemEffectFlagsSystem>().TrySetFlag(args.TargetEntity, ResistNeuroStatus))
            return;

        entities.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-neuroshielding-thick-skull"), args.TargetEntity, args.TargetEntity);
        entities.System<SharedStatusEffectsSystem>().TryRemoveStatusEffect(args.TargetEntity, "Dazed");
    }

    // TODO RMC14: brain damage on critical overdose - 2 * potency brain damage per 2s tick

    public static readonly EntProtoId ResistNeuroStatus = "RMCStatusEffectResistNeuro";
}
