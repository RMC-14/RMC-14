using System.Linq;
using Content.Shared._RMC14.Botany;
using Content.Shared.Atmos;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Random mutation & crosbreeding logic
/// </summary>
public sealed class RMCPlantMutationSystem : EntitySystem
{
    private static readonly ProtoId<WeightedRandomFillSolutionPrototype> RandomPickBotanyReagent = "RMCRandomPickBotanyReagent";

    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;

    private static readonly Color[] BioluminescentColors =
    [
        Color.FromHex("#FF0000"), Color.FromHex("#FF7F00"), Color.FromHex("#FFFF00"),
        Color.FromHex("#00FF00"), Color.FromHex("#0000FF"), Color.FromHex("#4B0082"),
        Color.FromHex("#8F00FF"),
    ];

    private static class Slot
    {
        public const string PlantCancer = "Plant Cancer";
        public const string Gluttony = "Gluttony";
        public const string Endurance = "Endurance";
        public const string ToxinTolerance = "Toxin Tolerance";
        public const string WeedTolerance = "Weed Tolerance";
        public const string LightTolerance = "Light Tolerance";
        public const string Production = "Production";
        public const string Lifespan = "Lifespan";
        public const string Potency = "Potency";
        public const string Maturity = "Maturity";
        public const string Bioluminescence = "Bioluminescence";
        public const string Flowers = "Flowers";
        public const string NewChems = "New Chems";
        public const string NewChems2 = "New Chems2";
        public const string NewChems3 = "New Chems3";
        public const string MutateSpecies = "Mutate Species";
    }

    private static readonly string[] MutationSlots =
    [
        Slot.PlantCancer,
        Slot.Gluttony,
        Slot.Endurance,
        Slot.ToxinTolerance,
        Slot.WeedTolerance,
        Slot.LightTolerance,
        Slot.Production,
        Slot.Lifespan,
        Slot.Potency,
        Slot.Maturity,
        Slot.Bioluminescence,
        Slot.Flowers,
        Slot.NewChems,
        Slot.NewChems2,
        Slot.NewChems3,
    ];

    /// <summary>
    /// Picks a random target from the plants own MutationPrototypes list & changes species to it
    /// </summary>
    public void RandomSpeciesChange(EntityUid plant)
    {
        if (!TryComp(plant, out RMCPlantMutationComponent? mutation) || mutation.MutationPrototypes.Count == 0)
            return;

        var targetProto = _robustRandom.Pick(mutation.MutationPrototypes);
        ChangeSpecies(plant, targetProto);
    }

    /// <summary>
    ///     Applies 1-3 random mutations filtered by the plants mutations slots
    /// </summary>
    public void MutatePlant(EntityUid plant, int degree)
    {
        if (!TryComp(plant, out RMCPlantMutationComponent? mutation) || mutation.MutationTier == RMCPlantMutationTier.Immutable)
            return;

        var controller = mutation.Slots;

        var speciesVal = controller.GetValueOrDefault(Slot.MutateSpecies, 0f);
        if (mutation.MutationPrototypes.Count > 0 && ((degree > 1 && speciesVal == 0f) || speciesVal > 0f))
        {
            var targetProto = _robustRandom.Pick(mutation.MutationPrototypes);
            ChangeSpecies(plant, targetProto);
            ResetController((plant, mutation));
            return;
        }

        var superAllowed = new List<string>();
        var normalAllowed = new List<string>();
        var cancelSlots = new HashSet<string>();

        foreach (var slotName in MutationSlots)
        {
            var val = controller.GetValueOrDefault(slotName, 0f);

            if (val > 0f)
                superAllowed.Add(slotName);
            else if (val >= -1f)
            {
                normalAllowed.Add(slotName);
                if (val == -1f)
                    cancelSlots.Add(slotName);
            }
            // val < -1 means suppressed — excluded from both lists
        }

        var candidates = superAllowed.Count > 0 ? superAllowed : normalAllowed;

        if (candidates.Count == 0)
        {
            ResetController((plant, mutation));
            return;
        }

        var mutationLevel = TryComp(plant, out RMCPlantComponent? plantComp) ? plantComp.MutationLevel : 0f;
        var iterations = _robustRandom.Next(1, degree + 2) + (int)MathF.Round(mutationLevel / 50f) + 1;

        for (var i = 0; i < iterations; i++)
        {
            var slot = _robustRandom.Pick(candidates);

            if (cancelSlots.Contains(slot))
                return;

            ApplyMutation(plant, slot, degree);
        }

        ResetController((plant, mutation));
    }

    private void ResetController(Entity<RMCPlantMutationComponent> mutation)
    {
        var changed = false;
        foreach (var key in mutation.Comp.Slots.Keys.ToList())
        {
            if (mutation.Comp.Slots[key] > -3f)
            {
                mutation.Comp.Slots[key] = 0f;
                changed = true;
            }
        }

        if (changed)
            _plantTray.DirtyMutationSlots((mutation.Owner, mutation.Comp));
    }

    private void ApplyMutation(EntityUid plant, string slot, int degree)
    {
        switch (slot)
        {
            case Slot.PlantCancer:
                if (TryComp(plant, out RMCPlantGrowthComponent? cancerGrowth))
                {
                    _plantTray.SetLifespan((plant, cancerGrowth), MathF.Max(0f, cancerGrowth.Lifespan - _robustRandom.Next(1, 6)));
                    _plantTray.SetEndurance((plant, cancerGrowth), MathF.Max(0f, cancerGrowth.Endurance - _robustRandom.Next(10, 21)));
                }
                break;

            case Slot.Gluttony:
                if (TryComp(plant, out RMCPlantMetabolismComponent? gluttony))
                {
                    _plantTray.SetMetabolismRates((plant, gluttony),
                        Math.Clamp(gluttony.NutrientConsumption + _robustRandom.NextFloat(-degree * 0.1f, degree * 0.1f), 0f, 5f),
                        Math.Clamp(gluttony.WaterConsumption + _robustRandom.NextFloat(-degree, degree), 0f, 50f));
                }
                break;

            case Slot.Endurance:
                if (TryComp(plant, out RMCPlantGrowthComponent? enduranceGrowth))
                {
                    _plantTray.SetEndurance((plant, enduranceGrowth), Math.Clamp(
                        enduranceGrowth.Endurance + _robustRandom.Next(-5, 6) * degree,
                        10f, 100f));
                }
                break;

            case Slot.ToxinTolerance:
                if (TryComp(plant, out RMCPlantTraitsComponent? toxinTraits))
                {
                    _plantTray.SetToxinsTolerance((plant, toxinTraits), Math.Clamp(
                        toxinTraits.ToxinsTolerance + _robustRandom.Next(-2, 3) * degree,
                        0f, 10f));
                }
                break;

            case Slot.WeedTolerance:
                if (TryComp(plant, out RMCPlantTraitsComponent? weedTraits))
                {
                    _plantTray.SetWeedTolerance((plant, weedTraits), Math.Clamp(
                        weedTraits.WeedTolerance + _robustRandom.Next(-2, 3) * degree,
                        0f, 10f));
                }

                if (_robustRandom.Prob(degree * 0.05f))
                {
                    var level = Math.Clamp(
                        (CompOrNull<RMCPlantTraitCarnivorousComponent>(plant)?.Level ?? 0) + _robustRandom.Next(-degree, degree + 1),
                        0, 2);
                    if (level <= 0)
                        RemComp<RMCPlantTraitCarnivorousComponent>(plant);
                    else
                    {
                        var carnivorous = EnsureComp<RMCPlantTraitCarnivorousComponent>(plant);
                        carnivorous.Level = level;
                        Dirty(plant, carnivorous);
                    }
                }
                else if (_robustRandom.Prob(degree * 0.05f))
                {
                    if (!RemComp<RMCPlantTraitParasiteComponent>(plant))
                        AddComp<RMCPlantTraitParasiteComponent>(plant);
                }
                break;

            case Slot.LightTolerance:
                if (TryComp(plant, out RMCPlantAtmosphericComponent? lightAtmos))
                {
                    _plantTray.SetLightTolerance((plant, lightAtmos),
                        Math.Clamp(lightAtmos.IdealLight + _robustRandom.Next(-1, 2) * degree, 0f, 30f),
                        Math.Clamp(lightAtmos.LightTolerance + _robustRandom.Next(-2, 3) * degree, 0f, 10f));
                }
                break;

            case Slot.Production:
                if (TryComp(plant, out RMCPlantGrowthComponent? productionGrowth))
                {
                    _plantTray.SetProduction((plant, productionGrowth), Math.Clamp(
                        productionGrowth.Production + _robustRandom.Next(-1, 2) * degree,
                        1f, 10f));
                }
                break;

            case Slot.Lifespan:
                if (TryComp(plant, out RMCPlantGrowthComponent? lifespanGrowth))
                {
                    _plantTray.SetLifespan((plant, lifespanGrowth), Math.Clamp(
                        lifespanGrowth.Lifespan + _robustRandom.Next(-2, 3) * degree,
                        10f, 30f));
                }
                if (TryComp(plant, out RMCPlantHarvestComponent? lifespanHarvest) && lifespanHarvest.Yield != -1)
                {
                    _plantTray.SetYield((plant, lifespanHarvest), Math.Clamp(lifespanHarvest.Yield + _robustRandom.Next(-2, 3) * degree, 0, 10));
                }
                break;

            case Slot.Potency:
                if (TryComp(plant, out RMCPlantChemicalsComponent? potencyChemicals))
                {
                    _plantTray.SetPotency((plant, potencyChemicals), Math.Clamp(
                        potencyChemicals.Potency + _robustRandom.Next(-20, 21) * degree,
                        0f, 200f));
                }
                break;

            case Slot.Maturity:
                if (TryComp(plant, out RMCPlantGrowthComponent? maturityGrowth))
                {
                    _plantTray.SetMaturation((plant, maturityGrowth), Math.Clamp(
                        maturityGrowth.Maturation + _robustRandom.Next(-1, 2) * degree,
                        0f, 30f));
                }
                if (_robustRandom.Prob(degree * 0.05f) && TryComp(plant, out RMCPlantHarvestComponent? maturityHarvest))
                {
                    _plantTray.SetHarvestRepeat((plant, maturityHarvest), maturityHarvest.HarvestRepeat == HarvestType.NoRepeat
                        ? HarvestType.Repeat
                        : HarvestType.NoRepeat);
                }
                break;

            case Slot.Bioluminescence:
                if (_robustRandom.Prob(degree * 0.02f) && !RemComp<RMCPlantTraitBioluminescentComponent>(plant))
                {
                    var biolum = AddComp<RMCPlantTraitBioluminescentComponent>(plant);
                    if (_robustRandom.Prob(degree * 0.02f))
                        biolum.Color = _robustRandom.Pick(BioluminescentColors);
                }
                break;

            case Slot.Flowers:
                if (_robustRandom.Prob(degree * 0.02f) && !RemComp<RMCPlantTraitFlowersComponent>(plant))
                {
                    var flowers = AddComp<RMCPlantTraitFlowersComponent>(plant);
                    if (_robustRandom.Prob(degree * 0.02f))
                        flowers.Color = _robustRandom.Pick(BioluminescentColors);
                }
                break;

            case Slot.NewChems:
            case Slot.NewChems2:
            case Slot.NewChems3:
                if (TryComp(plant, out RMCPlantChemicalsComponent? newChemsComp))
                    AddRandomChem((plant, newChemsComp));
                break;
        }
    }

    private void AddRandomChem(Entity<RMCPlantChemicalsComponent> plant)
    {
        var chemicals = plant.Comp;

        // 60u chem cap - no mutations after
        if (ChemOutputFull(chemicals))
            return;

        if (chemicals.SpecialChemicals.Count > 0 && _robustRandom.Prob(0.4f))
        {
            var specialId = _robustRandom.Pick(chemicals.SpecialChemicals);
            if (!chemicals.Chemicals.ContainsKey(specialId))
            {
                chemicals.Chemicals[specialId] = new SeedChemQuantity
                {
                    Min = 7,
                    Max = 15,
                    PotencyDivisor = _robustRandom.Next(5, 9),
                    Inherent = false,
                };
                _plantTray.DirtyChemicals((plant.Owner, plant.Comp));
            }

            return;
        }

        var fills = _prototype.Index(RandomPickBotanyReagent).Fills;
        if (fills.Count == 0)
            return;

        var pick = _robustRandom.Pick(fills);
        if (pick.Reagents.Count == 0)
            return;

        var chemicalId = _robustRandom.Pick(pick.Reagents);
        var amount = _robustRandom.Next(1, (int)pick.Quantity + 1);

        SeedChemQuantity seedChemQuantity;
        if (chemicals.Chemicals.TryGetValue(chemicalId, out var existing))
            seedChemQuantity = new SeedChemQuantity { Min = existing.Min, Max = existing.Max + amount, Inherent = existing.Inherent };
        else
            seedChemQuantity = new SeedChemQuantity { Min = 1, Max = 1 + amount, Inherent = false };

        seedChemQuantity.PotencyDivisor = (int)Math.Ceiling(100.0 / seedChemQuantity.Max);
        chemicals.Chemicals[chemicalId] = seedChemQuantity;
        _plantTray.DirtyChemicals((plant.Owner, plant.Comp));
    }

    private static bool ChemOutputFull(RMCPlantChemicalsComponent chemicals)
    {
        var total = 0f;
        foreach (var (_, q) in chemicals.Chemicals)
        {
            var amount = (float)q.Min;
            if (q.PotencyDivisor > 0 && chemicals.Potency > 0)
                amount += chemicals.Potency / q.PotencyDivisor;
            total += Math.Clamp(amount, q.Min, q.Max);
        }

        return total >= 60f;
    }

    private void ChangeSpecies(EntityUid plant, string targetProto)
    {
        if (!_prototype.TryIndex<EntityPrototype>(targetProto, out var proto))
            return;

        if (proto.TryGetComponent<RMCPlantComponent>(out var tempPlant, EntityManager.ComponentFactory) &&
            TryComp(plant, out RMCPlantComponent? plantComp))
            _plantTray.SetIdentity((plant, plantComp), tempPlant.Name, tempPlant.Noun, tempPlant.DisplayName, tempPlant.Mysterious);

        if (proto.TryGetComponent<RMCPlantHarvestComponent>(out var tempHarvest, EntityManager.ComponentFactory) &&
            TryComp(plant, out RMCPlantHarvestComponent? harvest))
            _plantTray.SetHarvestProducts((plant, harvest), tempHarvest.PacketPrototype, new List<string>(tempHarvest.ProductPrototypes));

        if (proto.TryGetComponent<RMCPlantGrowthComponent>(out var tempGrowth, EntityManager.ComponentFactory) &&
            TryComp(plant, out RMCPlantGrowthComponent? growth))
            _plantTray.SetGrowthStages((plant, growth), tempGrowth.GrowthStages);

        if (proto.TryGetComponent<RMCPlantTraitsComponent>(out var tempTraits, EntityManager.ComponentFactory) &&
            TryComp(plant, out RMCPlantTraitsComponent? traits))
            _plantTray.SetTraitCosmetics((plant, traits), tempTraits.PlantRsi, tempTraits.PlantIconState, tempTraits.SplatPrototype);

        if (proto.TryGetComponent<RMCPlantMutationComponent>(out var tempMutation, EntityManager.ComponentFactory) &&
            TryComp(plant, out RMCPlantMutationComponent? mutation))
            _plantTray.SetMutationPrototypes((plant, mutation), new List<string>(tempMutation.MutationPrototypes));

        if (proto.TryGetComponent<RMCPlantChemicalsComponent>(out var tempChemicals, EntityManager.ComponentFactory) &&
            TryComp(plant, out RMCPlantChemicalsComponent? chemicals))
        {
            chemicals.SpecialChemicals = new List<string>(tempChemicals.SpecialChemicals);

            // Adding the new chemicals from the new species
            foreach (var (chem, quantity) in tempChemicals.Chemicals)
                chemicals.Chemicals.TryAdd(chem, quantity);

            // Removing inherent chemicals from the old species. Leaving mutated/crossbred ones intact
            foreach (var chem in chemicals.Chemicals.Keys.ToList())
            {
                if (!tempChemicals.Chemicals.ContainsKey(chem) && chemicals.Chemicals[chem].Inherent)
                    chemicals.Chemicals.Remove(chem);
            }

            Dirty(plant, chemicals);
        }
    }

    /// <summary>
    /// Cross pollinates plant <paramref name="b"/> with the genetic snapshot <paramref name="a"/>
    /// (captured from another plant by a botany swab), mutating bs own components in place
    /// </summary>
    public void Cross(List<IComponent> a, EntityUid b)
    {
        if (Get<RMCPlantChemicalsComponent>(a) is { } aChem && TryComp(b, out RMCPlantChemicalsComponent? bChem))
        {
            CrossChemicals(bChem.Chemicals, aChem.Chemicals);
            CrossFloat(ref bChem.Potency, aChem.Potency);
            Dirty(b, bChem);
        }

        if (Get<RMCPlantMetabolismComponent>(a) is { } aMeta && TryComp(b, out RMCPlantMetabolismComponent? bMeta))
        {
            CrossFloat(ref bMeta.NutrientConsumption, aMeta.NutrientConsumption);
            CrossFloat(ref bMeta.WaterConsumption, aMeta.WaterConsumption);
            Dirty(b, bMeta);
        }

        if (Get<RMCPlantAtmosphericComponent>(a) is { } aAtmos && TryComp(b, out RMCPlantAtmosphericComponent? bAtmos))
        {
            CrossFloat(ref bAtmos.MinHeat, aAtmos.MinHeat);
            CrossFloat(ref bAtmos.MaxHeat, aAtmos.MaxHeat);
            CrossFloat(ref bAtmos.MinPressure, aAtmos.MinPressure);
            CrossFloat(ref bAtmos.MaxPressure, aAtmos.MaxPressure);
            Dirty(b, bAtmos);
        }

        if (Get<RMCPlantTraitsComponent>(a) is { } aTraits && TryComp(b, out RMCPlantTraitsComponent? bTraits))
        {
            CrossFloat(ref bTraits.ToxinsTolerance, aTraits.ToxinsTolerance);
            CrossFloat(ref bTraits.PestTolerance, aTraits.PestTolerance);
            CrossFloat(ref bTraits.WeedTolerance, aTraits.WeedTolerance);
            Dirty(b, bTraits);
        }

        CrossTraits(a, b);

        if (Get<RMCPlantGrowthComponent>(a) is { } aGrowth && TryComp(b, out RMCPlantGrowthComponent? bGrowth))
        {
            CrossFloat(ref bGrowth.Endurance, aGrowth.Endurance);
            CrossFloat(ref bGrowth.Lifespan, aGrowth.Lifespan);
            CrossFloat(ref bGrowth.Maturation, aGrowth.Maturation);
            CrossFloat(ref bGrowth.Production, aGrowth.Production);
            Dirty(b, bGrowth);
        }

        if (Get<RMCPlantHarvestComponent>(a) is { } aHarvest && TryComp(b, out RMCPlantHarvestComponent? bHarvest))
        {
            CrossInt(ref bHarvest.Yield, aHarvest.Yield);
            CrossBool(ref bHarvest.Seedless, aHarvest.Seedless);
            Dirty(b, bHarvest);
        }

        if (Get<RMCConsumeExudeGasComponent>(a) is { } aGas && TryComp(b, out RMCConsumeExudeGasComponent? bGas))
        {
            CrossGasses(bGas.ExudeGasses, aGas.ExudeGasses);
            CrossGasses(bGas.ConsumeGasses, aGas.ConsumeGasses);
            Dirty(b, bGas);
        }

        if (Get<RMCPlantMutationComponent>(a) is { } aMutation && TryComp(b, out RMCPlantMutationComponent? bMutation))
        {
            _plantTray.SetMutations((b, bMutation), bMutation.Mutations.Where(m => Random(0.5f))
                .Union(aMutation.Mutations.Where(m => Random(0.5f)))
                .DistinctBy(m => m.Name)
                .ToList());
        }

        // Hybrids have a high chance of being seedless
        if (Get<RMCPlantComponent>(a) is { } aPlant
            && TryComp(b, out RMCPlantComponent? bPlant)
            && aPlant.Name != bPlant.Name
            && Random(0.7f)
            && TryComp(b, out RMCPlantHarvestComponent? seedlessHarvest))
        {
            _plantTray.SetSeedless((b, seedlessHarvest), true);
        }
    }

    private static T? Get<T>(List<IComponent> snapshot) where T : class, IComponent
    {
        return snapshot.OfType<T>().FirstOrDefault();
    }

    private void CrossTraits(List<IComponent> a, EntityUid b)
    {
        foreach (var trait in a.OfType<RMCPlantTraitComponent>())
        {
            if (HasComp(b, trait.GetType()))
                continue;

            if (Random(0.5f))
                AddComp(b, _serialization.CreateCopy(trait, notNullableOverride: true));
        }
    }

    private void CrossChemicals(Dictionary<string, SeedChemQuantity> val, Dictionary<string, SeedChemQuantity> other)
    {
        // Go through chemicals from the pollen in swab
        foreach (var otherChem in other)
        {
            // if both have same chemical, randomly pick potency ratio from the two.
            if (val.ContainsKey(otherChem.Key))
            {
                val[otherChem.Key] = Random(0.5f) ? otherChem.Value : val[otherChem.Key];
            }
            // if target plant doesn't have this chemical, has 50% chance to add it.
            else if (Random(0.5f))
            {
                var fixedChem = otherChem.Value;
                fixedChem.Inherent = false;
                val.Add(otherChem.Key, fixedChem);
            }
        }

        // if the target plant has chemical that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisChem in val.ToList())
        {
            if (!other.ContainsKey(thisChem.Key) && Random(0.5f) && val.Count > 1)
                val.Remove(thisChem.Key);
        }
    }

    private void CrossGasses(Dictionary<Gas, float> val, Dictionary<Gas, float> other)
    {
        // Go through gasses from the pollen in swab
        foreach (var otherGas in other)
        {
            // if both have same gas, randomly pick amount from the two.
            if (val.ContainsKey(otherGas.Key))
            {
                val[otherGas.Key] = Random(0.5f) ? otherGas.Value : val[otherGas.Key];
            }
            // if target plant doesn't have this gas, has 50% chance to add it.
            else if (Random(0.5f))
            {
                val.Add(otherGas.Key, otherGas.Value);
            }
        }

        // if the target plant has gas that the pollen in swab does not, 50% chance to remove it.
        foreach (var thisGas in val.ToList())
        {
            if (!other.ContainsKey(thisGas.Key) && Random(0.5f))
                val.Remove(thisGas.Key);
        }
    }

    private void CrossFloat(ref float val, float other)
    {
        val = Random(0.5f) ? val : other;
    }

    private void CrossInt(ref int val, int other)
    {
        val = Random(0.5f) ? val : other;
    }

    private void CrossBool(ref bool val, bool other)
    {
        val = Random(0.5f) ? val : other;
    }

    private bool Random(float p)
    {
        return _robustRandom.Prob(p);
    }
}
