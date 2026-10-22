using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class XenoEggPlasmaEmbryo : EntityEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Develops into a parasitic embryo inside the host once it reaches critical overdose.";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent, Source: { } source, Method: null })
            return;

        var entities = args.EntityManager;
        var target = args.TargetEntity;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(target))
            return;

        if (entities.HasComponent<VictimInfectedComponent>(target) ||
            entities.HasComponent<SynthComponent>(target) ||
            !entities.HasComponent<InfectableComponent>(target))
        {
            RemoveAll(source, reagent);
            return;
        }

        if (reagent.CriticalOverdose is not { } threshold ||
            source.GetTotalPrototypeQuantity(reagent.ID) < threshold)
        {
            return;
        }

        var hive = entities.System<XenoEggPlasmaSystem>().GetEggPlasmaHive(source);
        RemoveAll(source, reagent);
        entities.System<SharedXenoParasiteSystem>().Infect(target, hive);

        entities.System<SharedPopupSystem>().PopupEntity(
            Loc.GetString("chem-egg-plasma-infected"),
            target,
            target,
            PopupType.MediumCaution);
    }

    private static void RemoveAll(Solution source, ReagentPrototype reagent)
    {
        var quantity = source.GetTotalPrototypeQuantity(reagent.ID);
        source.RemoveReagent(new ReagentQuantity(reagent.ID, quantity), ignoreReagentData: true);
    }
}
