using System.Linq;
using Content.Server._RMC14.Botany;
using Content.Server.Popups;
using Content.Shared._RMC14.Botany;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Produce generation
/// </summary>
public sealed class RMCPlantProduceSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCProduceComponent, ExaminedEvent>(OnProduceExamined);
    }

    public IEnumerable<EntityUid> Harvest(EntityUid plant, EntityUid user, float yieldMod)
    {
        var harvest = Comp<RMCPlantHarvestComponent>(plant);
        if (harvest.ProductPrototypes.Count == 0 || harvest.Yield <= 0)
        {
            _popup.PopupCursor(Loc.GetString("botany-harvest-fail-message"), user, PopupType.Medium);
            return Enumerable.Empty<EntityUid>();
        }

        var name = Loc.GetString(Comp<RMCPlantComponent>(plant).DisplayName);
        _popup.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);

        if (TryComp(plant, out RMCPlantLogComponent? log) && log.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, log.HarvestLogImpact.Value, $"{ToPrettyString(user):player} harvested {name}.");

        return GenerateProduct(plant, Transform(user).Coordinates, yieldMod);
    }

    public IEnumerable<EntityUid> AutoHarvest(EntityUid plant, EntityCoordinates position, float yieldMod)
    {
        var harvest = Comp<RMCPlantHarvestComponent>(plant);
        if (!position.IsValid(EntityManager) || harvest.ProductPrototypes.Count == 0)
            return Enumerable.Empty<EntityUid>();

        if (TryComp(plant, out RMCPlantLogComponent? log) && log.HarvestLogImpact != null)
        {
            var name = Loc.GetString(Comp<RMCPlantComponent>(plant).DisplayName);
            _adminLogger.Add(LogType.Botany, log.HarvestLogImpact.Value, $"Auto-harvested {name} at Pos:{position}.");
        }

        return GenerateProduct(plant, position, yieldMod);
    }

    public IEnumerable<EntityUid> GenerateProduct(EntityUid plant, EntityCoordinates position, float yieldMod)
    {
        var harvest = Comp<RMCPlantHarvestComponent>(plant);

        var totalYield = 0;
        if (harvest.Yield > -1)
        {
            if (yieldMod < 1f)
            {
                yieldMod = 0f;
                totalYield = harvest.Yield;
            }
            else
            {
                totalYield = harvest.Yield + _robustRandom.Next(1, (int) yieldMod + 1);
            }

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();
        var plantProtoId = MetaData(plant).EntityPrototype?.ID;
        var plantComp = Comp<RMCPlantComponent>(plant);
        var chemicals = Comp<RMCPlantChemicalsComponent>(plant);

        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(harvest.ProductPrototypes);

            var entity = Spawn(product, position);
            _randomHelper.RandomOffset(entity, 0.25f);
            products.Add(entity);

            var produce = EnsureComp<RMCProduceComponent>(entity);
            produce.PlantPrototype = plantProtoId;
            produce.Name = plantComp.Name;
            produce.Noun = plantComp.Noun;
            Dirty(entity, produce);

            // Each has own copy
            _plantSeed.ApplyMutatedComponents(entity, _plantSeed.SnapshotMutableComponents(plant));

            ProduceGrown(entity, produce);

            if (HasComp<RMCPlantTraitBioluminescentComponent>(entity))
            {
                var light = _pointLight.EnsureLight(entity);
                _pointLight.SetRadius(entity, 1f, light);
                _pointLight.SetEnabled(entity, true, light);
            }

            _appearance.SetData(entity, RMCProduceVisuals.Potency, chemicals.Potency);
            if (chemicals.ProductColor is { } productColor)
                _appearance.SetData(entity, RMCProduceVisuals.Color, productColor);

            if (plantComp.Mysterious)
            {
                var metaData = MetaData(entity);
                _metaData.SetEntityName(entity, metaData.EntityName + "?", metaData);
                _metaData.SetEntityDescription(entity,
                    metaData.EntityDescription + " " + Loc.GetString("botany-mysterious-description-addon"), metaData);
            }
        }

        return products;
    }

    public void ProduceGrown(EntityUid uid, RMCProduceComponent produce)
    {
        if (!TryComp(uid, out RMCPlantMutationComponent? mutation))
            return;

        foreach (var effect in mutation.Mutations)
        {
            if (effect.AppliesToProduce)
            {
                var args = new EntityEffectBaseArgs(uid, EntityManager);
                effect.Effect.Effect(args);
            }
        }

        if (!_solutionContainer.EnsureSolution(uid, produce.SolutionName, out var solutionContainer, FixedPoint2.Zero))
            return;

        if (!TryComp<RMCPlantChemicalsComponent>(uid, out var chemicals))
            return;

        var maxProduceVolume = FixedPoint2.New(50);
        solutionContainer.RemoveAllSolution();
        solutionContainer.MaxVolume = maxProduceVolume;
        foreach (var (chem, quantity) in chemicals.Chemicals)
        {
            var available = solutionContainer.AvailableVolume;
            if (available <= FixedPoint2.Zero)
                break;

            var amount = FixedPoint2.New(RMCPlantSeedSystem.GetChemicalAmount(quantity, chemicals.Potency));
            if (amount > available)
                amount = available;
            solutionContainer.AddReagent(chem, amount);
        }
    }

    private void OnProduceExamined(Entity<RMCProduceComponent> produce, ref ExaminedEvent args)
    {
        if (!TryComp(produce.Owner, out RMCPlantMutationComponent? mutation))
            return;

        using (args.PushGroup(nameof(RMCProduceComponent)))
        {
            foreach (var effect in mutation.Mutations)
            {
                if (!effect.AppliesToProduce)
                    continue;

                if (effect.Description != null)
                    args.PushMarkup(Loc.GetString(effect.Description));
            }
        }
    }
}
