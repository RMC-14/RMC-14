using System.Linq;
using Content.Shared._RMC14.Botany;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class AcidBloodMutating : RMCChemicalEffect
{
    [DataField]
    public bool Royal;

    [DataField]
    public ProtoId<ReagentPrototype> SelfChem = "RMCXenoBlood";

    [DataField]
    public float MutateChance = 0.1f;

    [DataField]
    public float RoyalChemAddThreshold = 5f;

    [DataField]
    public float RoyalChemAddChance = 0.6f;

    [DataField]
    public int MaxChemicals = 10;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Corrodes plants in a hydroponics tray and scrambles the chemicals they produce.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<RMCPlantChemicalsComponent>(plant.Owner, out var chemicals))
            return;

        // cm toxins += 3*volume (royal 6), health -= volume (royal 4*volume)
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        var p = (float) args.Quantity;
        if (plant.Comp.Tray is { } trayUid && GetTray(args.EntityManager, plant) is { } tray)
            plantTray.AdjustToxins((trayUid, tray), (Royal ? 6f : 3f) * p);
        plantTray.AdjustHealth((plant.Owner, plant.Comp, null), -(Royal ? 4f : 1f) * p);

        var random = IoCManager.Resolve<IRobustRandom>();

        if (Royal)
        {
            var counter = chemicals.ChemAddCounter + p;
            plantTray.SetChemAddCounter((plant.Owner, chemicals), counter);

            if (counter < RoyalChemAddThreshold || !random.Prob(RoyalChemAddChance))
                return;

            plantTray.SetChemAddCounter((plant.Owner, chemicals), counter - RoyalChemAddThreshold);

            if (chemicals.Chemicals.Count > MaxChemicals || !chemicals.Chemicals.ContainsKey(SelfChem))
                return;

            var prototype = IoCManager.Resolve<IPrototypeManager>();
            var hydro = prototype.EnumeratePrototypes<ReagentPrototype>()
                .Where(r => !r.Abstract && r.ChemClass == ChemClass.Hydro)
                .Select(r => r.ID)
                .ToList();

            if (hydro.Count == 0)
                return;

            var pick = random.Pick(hydro);
            chemicals.Chemicals.TryAdd(pick, new SeedChemQuantity { Min = 1, Max = random.Next(2, 4), PotencyDivisor = 20, Inherent = false });
            plantTray.DirtyChemicals((plant.Owner, chemicals));
            return;
        }

        if (!random.Prob(MutateChance))
            return;

        if (chemicals.Chemicals.Count > 1)
        {
            var removed = random.Pick(chemicals.Chemicals.Keys.ToList());
            chemicals.Chemicals.Remove(removed);
            plantTray.DirtyChemicals((plant.Owner, chemicals));
        }
        else
        {
            chemicals.Chemicals.TryAdd(SelfChem, new SeedChemQuantity { Min = 1, Max = 2, PotencyDivisor = 20, Inherent = false });
            plantTray.DirtyChemicals((plant.Owner, chemicals));
        }
    }
}
