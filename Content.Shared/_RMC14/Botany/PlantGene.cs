using System.Linq;
using Content.Shared.Atmos;
using Content.Shared._RMC14.Botany;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Botany;

public sealed class PlantGene
{
    public PlantGeneType Type;

    public string? DisplayLabel;

    public List<string>? ProductPrototypes;
    public Dictionary<string, SeedChemQuantity>? Chemicals;
    public Dictionary<Gas, float>? ExudeGasses;
    public float? AlterTemperature;
    public float? Potency;
    public HarvestType? HarvestRepeat;

    public Dictionary<Gas, float>? ConsumeGasses;
    public float? NutrientConsumption;
    public float? WaterConsumption;
    public int? Carnivorous;
    public bool? Parasite;

    public float? MinHeat;
    public float? MaxHeat;
    public float? MinPressure;
    public float? MaxPressure;

    public float? ToxinsTolerance;
    public float? PestTolerance;
    public float? WeedTolerance;

    public float? Endurance;
    public int? Yield;
    public float? Lifespan;
    public float? Maturation;
    public float? Production;

    public ResPath? PlantRsi;
    public int? GrowthStages;
    public string? PlantIconState;
    public Color? ProductColor;
    public bool? HasFlowers;
    public string? FlowerIcon;
    public Color? FlowerColor;
    public bool? Bioluminescent;
    public Color? BioluminescentColor;

    public static PlantGene FromSnapshot(List<IComponent> snapshot, PlantGeneType type)
    {
        var gene = new PlantGene { Type = type };

        var harvest = Get<RMCPlantHarvestComponent>(snapshot);
        var chemicals = Get<RMCPlantChemicalsComponent>(snapshot);
        var gas = Get<RMCConsumeExudeGasComponent>(snapshot);
        var atmos = Get<RMCPlantAtmosphericComponent>(snapshot);
        var metabolism = Get<RMCPlantMetabolismComponent>(snapshot);
        var traits = Get<RMCPlantTraitsComponent>(snapshot);
        var growth = Get<RMCPlantGrowthComponent>(snapshot);

        switch (type)
        {
            case PlantGeneType.Products:
                if (harvest != null)
                {
                    gene.ProductPrototypes = new List<string>(harvest.ProductPrototypes);
                    gene.HarvestRepeat = harvest.HarvestRepeat;
                }
                if (chemicals != null)
                {
                    gene.Chemicals = new Dictionary<string, SeedChemQuantity>(chemicals.Chemicals);
                    gene.Potency = chemicals.Potency;
                }
                if (gas != null)
                    gene.ExudeGasses = new Dictionary<Gas, float>(gas.ExudeGasses);
                if (atmos != null)
                    gene.AlterTemperature = atmos.AlterTemperature;
                break;

            case PlantGeneType.Consumption:
                if (gas != null)
                    gene.ConsumeGasses = new Dictionary<Gas, float>(gas.ConsumeGasses);
                if (metabolism != null)
                {
                    gene.NutrientConsumption = metabolism.NutrientConsumption;
                    gene.WaterConsumption = metabolism.WaterConsumption;
                }
                gene.Carnivorous = Get<RMCPlantTraitCarnivorousComponent>(snapshot)?.Level ?? 0;
                gene.Parasite = Get<RMCPlantTraitParasiteComponent>(snapshot) != null;
                break;

            case PlantGeneType.Environment:
                if (atmos != null)
                {
                    gene.MinHeat = atmos.MinHeat;
                    gene.MaxHeat = atmos.MaxHeat;
                    gene.MinPressure = atmos.MinPressure;
                    gene.MaxPressure = atmos.MaxPressure;
                }
                break;

            case PlantGeneType.Resistance:
                if (traits != null)
                {
                    gene.ToxinsTolerance = traits.ToxinsTolerance;
                    gene.PestTolerance = traits.PestTolerance;
                    gene.WeedTolerance = traits.WeedTolerance;
                }
                break;

            case PlantGeneType.Vigour:
                if (growth != null)
                {
                    gene.Endurance = growth.Endurance;
                    gene.Lifespan = growth.Lifespan;
                    gene.Maturation = growth.Maturation;
                    gene.Production = growth.Production;
                }
                if (harvest != null)
                    gene.Yield = harvest.Yield;
                break;

            case PlantGeneType.Flowers:
                if (traits != null)
                {
                    gene.PlantRsi = traits.PlantRsi;
                    gene.PlantIconState = traits.PlantIconState;
                }

                var flowers = Get<RMCPlantTraitFlowersComponent>(snapshot);
                gene.HasFlowers = flowers != null;
                gene.FlowerIcon = flowers?.Icon;
                gene.FlowerColor = flowers?.Color;

                var bioluminescent = Get<RMCPlantTraitBioluminescentComponent>(snapshot);
                gene.Bioluminescent = bioluminescent != null;
                gene.BioluminescentColor = bioluminescent?.Color ?? Color.White;

                if (growth != null)
                    gene.GrowthStages = growth.GrowthStages;
                if (chemicals != null)
                    gene.ProductColor = chemicals.ProductColor;
                break;
        }

        return gene;
    }

    public void ApplyTo(List<IComponent> snapshot)
    {
        switch (Type)
        {
            case PlantGeneType.Products:
                if (Chemicals != null)
                {
                    var chemicals = GetOrCreate(snapshot, () => new RMCPlantChemicalsComponent());
                    foreach (var (reagentId, incoming) in Chemicals)
                    {
                        if (chemicals.Chemicals.TryGetValue(reagentId, out var existing))
                        {
                            chemicals.Chemicals[reagentId] = new SeedChemQuantity
                            {
                                Min = existing.Min != 0 ? Math.Max(1, (existing.Min + incoming.Min) / 2) : incoming.Min,
                                PotencyDivisor = incoming.PotencyDivisor == 0
                                    ? existing.PotencyDivisor
                                    : existing.PotencyDivisor != 0
                                        ? Math.Max(1, (existing.PotencyDivisor + incoming.PotencyDivisor) / 2)
                                        : incoming.PotencyDivisor,
                                Inherent = incoming.Inherent,
                            };
                        }
                        else
                        {
                            chemicals.Chemicals[reagentId] = incoming;
                        }
                    }

                    if (Potency is { } potency)
                        chemicals.Potency = potency;
                }

                if (ProductPrototypes != null)
                {
                    var harvestProducts = GetOrCreate(snapshot, () => new RMCPlantHarvestComponent());
                    foreach (var product in ProductPrototypes)
                    {
                        if (!harvestProducts.ProductPrototypes.Contains(product))
                            harvestProducts.ProductPrototypes.Add(product);
                    }
                }

                if (ExudeGasses != null)
                {
                    var gas = GetOrCreate(snapshot, () => new RMCConsumeExudeGasComponent());
                    foreach (var (gasType, amount) in ExudeGasses)
                        gas.ExudeGasses[gasType] = MathF.Max(1f, amount * 0.8f);
                }

                if (AlterTemperature is { } alterTemperature)
                    GetOrCreate(snapshot, () => new RMCPlantAtmosphericComponent()).AlterTemperature = alterTemperature;

                if (HarvestRepeat.HasValue)
                    GetOrCreate(snapshot, () => new RMCPlantHarvestComponent()).HarvestRepeat = HarvestRepeat.Value;

                // Penalty for splicing in foreign chemistry (-15% vigour stats)
                var growth = GetOrCreate(snapshot, () => new RMCPlantGrowthComponent());
                growth.Endurance = MathF.Max(1f, growth.Endurance * 0.85f);
                growth.Lifespan = MathF.Max(1f, growth.Lifespan * 0.85f);
                var harvestYield = GetOrCreate(snapshot, () => new RMCPlantHarvestComponent());
                harvestYield.Yield = Math.Max(1, (int)(harvestYield.Yield * 0.85f));
                break;

            case PlantGeneType.Consumption:
                var metabolism = GetOrCreate(snapshot, () => new RMCPlantMetabolismComponent());
                if (ConsumeGasses != null)
                    GetOrCreate(snapshot, () => new RMCConsumeExudeGasComponent()).ConsumeGasses = new Dictionary<Gas, float>(ConsumeGasses);
                metabolism.NutrientConsumption = NutrientConsumption!.Value;
                metabolism.WaterConsumption = WaterConsumption!.Value;
                if (Carnivorous is { } carnivorous)
                {
                    if (carnivorous > 0)
                        GetOrCreate(snapshot, () => new RMCPlantTraitCarnivorousComponent()).Level = carnivorous;
                    else
                        Remove<RMCPlantTraitCarnivorousComponent>(snapshot);
                }
                if (Parasite is { } parasite)
                {
                    if (parasite)
                        GetOrCreate(snapshot, () => new RMCPlantTraitParasiteComponent());
                    else
                        Remove<RMCPlantTraitParasiteComponent>(snapshot);
                }
                break;

            case PlantGeneType.Environment:
                var atmos = GetOrCreate(snapshot, () => new RMCPlantAtmosphericComponent());
                atmos.MinHeat = MinHeat!.Value;
                atmos.MaxHeat = MaxHeat!.Value;
                atmos.MinPressure = MinPressure!.Value;
                atmos.MaxPressure = MaxPressure!.Value;
                break;

            case PlantGeneType.Resistance:
                var traits = GetOrCreate(snapshot, () => new RMCPlantTraitsComponent());
                traits.ToxinsTolerance = ToxinsTolerance!.Value;
                traits.PestTolerance = PestTolerance!.Value;
                traits.WeedTolerance = WeedTolerance!.Value;
                break;

            case PlantGeneType.Vigour:
                var growthV = GetOrCreate(snapshot, () => new RMCPlantGrowthComponent());
                growthV.Endurance = Endurance!.Value;
                growthV.Lifespan = Lifespan!.Value;
                growthV.Maturation = Maturation!.Value;
                growthV.Production = Production!.Value;
                GetOrCreate(snapshot, () => new RMCPlantHarvestComponent()).Yield = Yield!.Value;
                break;

            case PlantGeneType.Flowers:
                var traitsF = GetOrCreate(snapshot, () => new RMCPlantTraitsComponent());
                traitsF.PlantRsi = PlantRsi!.Value;
                traitsF.PlantIconState = PlantIconState!;

                if (HasFlowers is { } hasFlowers)
                {
                    if (hasFlowers)
                    {
                        var flowerComp = GetOrCreate(snapshot, () => new RMCPlantTraitFlowersComponent());
                        flowerComp.Icon = FlowerIcon;
                        flowerComp.Color = FlowerColor;
                    }
                    else
                    {
                        Remove<RMCPlantTraitFlowersComponent>(snapshot);
                    }
                }

                if (Bioluminescent is { } bioluminescent)
                {
                    if (bioluminescent)
                    {
                        var biolumComp = GetOrCreate(snapshot, () => new RMCPlantTraitBioluminescentComponent());
                        if (BioluminescentColor is { } bioluminescentColor)
                            biolumComp.Color = bioluminescentColor;
                    }
                    else
                    {
                        Remove<RMCPlantTraitBioluminescentComponent>(snapshot);
                    }
                }

                GetOrCreate(snapshot, () => new RMCPlantGrowthComponent()).GrowthStages = GrowthStages!.Value;
                if (ProductColor != null)
                    GetOrCreate(snapshot, () => new RMCPlantChemicalsComponent()).ProductColor = ProductColor;
                break;
        }
    }

    public string GetLabel()
    {
        return DisplayLabel is { } displayLabel ? $"{displayLabel} ({Type.GetLabel()})" : Type.GetLabel();
    }

    private static T? Get<T>(List<IComponent> snapshot) where T : class, IComponent
    {
        return snapshot.OfType<T>().FirstOrDefault();
    }

    private static T GetOrCreate<T>(List<IComponent> snapshot, Func<T> create) where T : class, IComponent
    {
        if (Get<T>(snapshot) is { } existing)
            return existing;

        var created = create();
        snapshot.Add(created);
        return created;
    }

    private static void Remove<T>(List<IComponent> snapshot) where T : class, IComponent
    {
        snapshot.RemoveAll(c => c is T);
    }
}
