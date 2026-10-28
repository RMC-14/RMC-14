using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Photosensitive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Not safe to administer. Supercharges photosynthesis, treated plants may become able to be harvested repeatedly.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<RMCPlantHarvestComponent>(plant.Owner, out var harvest) || harvest.HarvestRepeat == HarvestType.Repeat)
            return;

        var amount = (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustWeedLevel((trayUid, tray), amount * 0.25f);
            plantTray.AdjustNutritionLevel((trayUid, tray), -amount * 0.25f);
        }

        plantTray.SetRepeatHarvestCounter((plant.Owner, harvest), harvest.RepeatHarvestCounter + amount * 10f);

        if (harvest.RepeatHarvestCounter < 100f)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f))
        {
            plantTray.SetRepeatHarvestCounter((plant.Owner, harvest), harvest.RepeatHarvestCounter - random.Next(20, 51));
            return;
        }

        plantTray.SetHarvestRepeat((plant.Owner, harvest), HarvestType.Repeat);
        plantTray.SetRepeatHarvestCounter((plant.Owner, harvest), 0f);

        if (args.EntityManager.TryGetComponent<RMCPlantChemicalsComponent>(plant.Owner, out var chemicals))
            plantTray.SetPotencyCounter((plant.Owner, chemicals), 0f);

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("plant-repeat-harvest-shimmer", ("name", Loc.GetString(plant.Comp.DisplayName))), args.TargetEntity);
    }

    // TODO RMC14: brain organ damage - potency brain organ damage per 2s tick

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f))
        {
            var popup = args.EntityManager.System<SharedPopupSystem>();
            popup.PopupEntity(Loc.GetString("chem-photosensitive-migraine"), args.TargetEntity, args.TargetEntity, PopupType.SmallCaution);
        }
    }
}
