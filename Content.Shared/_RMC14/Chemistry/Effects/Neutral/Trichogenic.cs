using System.Linq;
using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Trichogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Increases plant yield at the cost of nutrients and water.\n" +
               "Overdoses may cause hair to grow inside the body.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.025f * (float) ActualPotency))
            return;

        if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var humanoid))
            return;

        var markingManager = IoCManager.Resolve<MarkingManager>();
        var appearance = args.EntityManager.System<SharedHumanoidAppearanceSystem>();
        var changed = false;

        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, humanoid.Species).Keys.ToList();
        if (hairStyles.Count > 0)
        {
            humanoid.MarkingSet.RemoveCategory(MarkingCategories.Hair);
            appearance.AddMarking(args.TargetEntity, random.Pick(hairStyles), forced: true, humanoid: humanoid);
            changed = true;
        }

        if (humanoid.Sex)
        {
            var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, humanoid.Species).Keys.ToList();
            if (facialHairStyles.Count > 0)
            {
                humanoid.MarkingSet.RemoveCategory(MarkingCategories.FacialHair);
                appearance.AddMarking(args.TargetEntity, random.Pick(facialHairStyles), forced: true, humanoid: humanoid);
                changed = true;
            }
        }

        if (!changed)
            return;

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("chem-trichogenic-hair-change"), args.TargetEntity);
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var scaled = (float) ActualPotency * 2f * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        AddYieldMod(args.EntityManager, plant, 0.2f * scaled);

        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustNutritionLevel((trayUid, tray), -0.5f * scaled);
            plantTray.AdjustWaterLevel((trayUid, tray), -0.1f * scaled);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.025f * (float) ActualPotency))
            return;

        args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-trichogenic-itchy"), args.TargetEntity, args.TargetEntity, PopupType.SmallCaution);

        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, ActualPotency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IoCManager.Resolve<IRobustRandom>().Prob(0.5f))
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-trichogenic-skull"), args.TargetEntity, args.TargetEntity, PopupType.MediumCaution);

        // TODO RMC14: brain damage on critical overdose, "hair growing into brain"
    }
}
