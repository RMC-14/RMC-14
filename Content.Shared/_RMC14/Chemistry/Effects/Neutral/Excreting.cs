using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Excreting : RMCChemicalEffect
{
    [DataField]
    public float ToxinsAmount = 1.5f;

    [DataField]
    public float WeedsAmount = 1f;

    [DataField]
    public float CounterIncrement = 5f;

    [DataField]
    public float NutrientConsumptionIncrease = 0.3f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the plant to secrete excess growth hormones, gradually increasing potency.\n" +
               "On mobs, purges other chemicals from the bloodstream.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        if (!bloodstream.TryGetChemicalSolution(args.TargetEntity, out var solutionEnt, out _))
            return;

        var solutionContainer = args.EntityManager.System<SharedSolutionContainerSystem>();
        solutionContainer.RemoveEachReagent(solutionEnt, Potency);
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<RMCPlantChemicalsComponent>(plant.Owner, out var chemicals))
            return;

        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        var scaled = (float) ActualPotency * 2f * (float) args.Quantity;
        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
        {
            plantTray.AdjustToxins((trayUid, tray), ToxinsAmount * scaled);
            plantTray.AdjustWeedLevel((trayUid, tray), WeedsAmount * scaled);
        }

        plantTray.SetPotencyCounter((plant.Owner, chemicals), chemicals.PotencyCounter + CounterIncrement * scaled);

        if (chemicals.PotencyCounter < 100f)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var level = Math.Max((int) (ActualPotency * 2f), 1);

        if (random.Next(0, level + 1) <= 0)
            return;

        plantTray.SetPotency((plant.Owner, chemicals), chemicals.Potency + random.Next(1, level + 1));

        if (args.EntityManager.TryGetComponent<RMCPlantMetabolismComponent>(plant.Owner, out var metabolism))
        {
            plantTray.SetMetabolismRates((plant.Owner, metabolism),
                metabolism.NutrientConsumption + NutrientConsumptionIncrease * (float) (ActualPotency * 2f), metabolism.WaterConsumption);
        }

        plantTray.SetPotencyCounter((plant.Owner, chemicals), 0f);
        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("plant-excreting-potency-boost"), args.TargetEntity);
    }
}
