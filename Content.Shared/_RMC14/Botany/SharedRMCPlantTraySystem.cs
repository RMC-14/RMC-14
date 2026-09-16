using System.Linq;
using Content.Shared.Burial.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Botany;

public abstract class SharedRMCPlantTraySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantTrayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCPlantTrayComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RMCPlantTrayComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCPlantTrayComponent, SolutionTransferredEvent>(OnSolutionTransferred);
    }

    private void OnInit(Entity<RMCPlantTrayComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PlantSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.PlantSlotId);
        ent.Comp.PlantSlot.ShowContents = true;
    }

    private void OnExamine(Entity<RMCPlantTrayComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var tray = entity.Comp;
        var plant = tray.PlantSlot.ContainedEntity;

        using (args.PushGroup(nameof(RMCPlantTrayComponent)))
        {
            if (plant == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else
            {
                var comp = Comp<RMCPlantComponent>(plant.Value);
                var growth = Comp<RMCPlantGrowthComponent>(plant.Value);

                if (!comp.Dead)
                {
                    var displayName = Loc.GetString(comp.DisplayName);
                    args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                        ("seedName", displayName),
                        ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

                    if (comp.Health <= growth.Endurance / 2)
                    {
                        args.PushMarkup(Loc.GetString(
                            "plant-holder-component-something-already-growing-low-health-message",
                            ("healthState",
                                Loc.GetString(comp.Age > growth.Lifespan
                                    ? "plant-holder-component-plant-old-adjective"
                                    : "plant-holder-component-plant-unhealthy-adjective"))));
                    }

                    foreach (var trait in EntityManager.GetComponents(plant.Value).OfType<RMCPlantTraitComponent>()
                                 .OrderBy(t => t.GetType().Name))
                    {
                        if (trait.TraitState is { } traitState)
                            args.PushMarkup(Loc.GetString(traitState));
                    }

                    if (!growth.Viable)
                        args.PushMarkup(Loc.GetString("mutation-plant-unviable"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
                }
            }

            if (tray.WeedLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-weed-high-level-message"));

            if (tray.PestLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-pest-high-level-message"));

            args.PushMarkup(Loc.GetString("plant-holder-component-water-level-message",
                ("waterLevel", (int)tray.WaterLevel)));
            args.PushMarkup(Loc.GetString("plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int)tray.NutritionLevel)));

            if (tray.DrawWarnings)
            {
                if (tray.Toxins >= 5)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (tray.ImproperHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));

                if (tray.ImproperPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning"));

                if (tray.MissingGas > 0)
                    args.PushMarkup(Loc.GetString("plant-holder-component-gas-missing-warning"));
            }
        }
    }

    private void OnInteractUsing(Entity<RMCPlantTrayComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var (uid, tray) = entity;

        if (HasComp<RMCHoeToolComponent>(args.Used))
        {
            args.Handled = true;
            if (tray.WeedLevel > 0)
            {
                _popup.PopupPredicted(
                    Loc.GetString("plant-holder-component-remove-weeds-message", ("name", Comp<MetaDataComponent>(uid).EntityName)),
                    Loc.GetString("plant-holder-component-remove-weeds-others-message", ("otherName", Comp<MetaDataComponent>(args.User).EntityName)),
                    uid, args.User, PopupType.Medium);
                SetWeedLevel((uid, tray), 0);
                UpdateSprite(uid, tray);
            }
            else
            {
                _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            }

            return;
        }

        if (HasComp<ShovelComponent>(args.Used))
        {
            args.Handled = true;
            if (tray.PlantSlot.ContainedEntity != null)
            {
                _popup.PopupPredicted(
                    Loc.GetString("plant-holder-component-remove-plant-message", ("name", Comp<MetaDataComponent>(uid).EntityName)),
                    Loc.GetString("plant-holder-component-remove-plant-others-message", ("name", Comp<MetaDataComponent>(args.User).EntityName)),
                    uid, args.User, PopupType.Medium);
                RemovePlant(uid, tray);
            }
            else
            {
                _popup.PopupPredictedCursor(
                    Loc.GetString("plant-holder-component-no-plant-message", ("name", Comp<MetaDataComponent>(uid).EntityName)),
                    args.User);
            }

            return;
        }

        InteractUsingUnhandled(entity, ref args);
    }

    protected virtual void InteractUsingUnhandled(Entity<RMCPlantTrayComponent> entity, ref InteractUsingEvent args)
    {
    }

    private void OnSolutionTransferred(Entity<RMCPlantTrayComponent> ent, ref SolutionTransferredEvent args)
    {
        _audio.PlayPredicted(ent.Comp.WateringSound, ent, args.User);
    }

    public void AdjustHealth(Entity<RMCPlantComponent?, RMCPlantGrowthComponent?> plant, float amount)
    {
        if (!Resolve(plant.Owner, ref plant.Comp1, ref plant.Comp2))
            return;

        plant.Comp1.Health = MathHelper.Clamp(plant.Comp1.Health + amount, 0f, plant.Comp2.Endurance);
        DirtyField(plant.Owner, plant.Comp1, nameof(RMCPlantComponent.Health));
        CheckHealth(plant.Owner, plant.Comp1);
    }

    protected virtual void CheckHealth(EntityUid plant, RMCPlantComponent comp)
    {
    }

    public void AdjustMutationLevel(Entity<RMCPlantComponent?> plant, float amount)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.MutationLevel = MathF.Max(0f, plant.Comp.MutationLevel + amount);
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.MutationLevel));
    }

    public void AdjustMutationMod(Entity<RMCPlantComponent?> plant, float amount)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.MutationMod += amount;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.MutationMod));
    }

    public void SetSampled(Entity<RMCPlantComponent?> plant, bool value = true)
    {
        if (!Resolve(plant.Owner, ref plant.Comp) || plant.Comp.Sampled == value)
            return;

        plant.Comp.Sampled = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.Sampled));
    }

    public void AdjustAge(Entity<RMCPlantComponent?> plant, int amount)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Age = Math.Max(0, plant.Comp.Age + amount);
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.Age));
    }

    public void AdjustSkipAging(Entity<RMCPlantComponent?> plant, int amount)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.SkipAging = Math.Max(0, plant.Comp.SkipAging + amount);
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.SkipAging));
    }

    public void SetMutationLevel(Entity<RMCPlantComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp) || plant.Comp.MutationLevel == value)
            return;

        plant.Comp.MutationLevel = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.MutationLevel));
    }

    public void SetMutationMod(Entity<RMCPlantComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp) || plant.Comp.MutationMod == value)
            return;

        plant.Comp.MutationMod = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.MutationMod));
    }

    public void SetYieldMod(Entity<RMCPlantComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp) || plant.Comp.YieldMod == value)
            return;

        plant.Comp.YieldMod = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.YieldMod));
    }

    public void SetMetabolismAdjust(Entity<RMCPlantComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.MetabolismAdjust = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.MetabolismAdjust));
    }

    public void SetBlightProductionCounter(Entity<RMCPlantComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.BlightProductionCounter = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.BlightProductionCounter));
    }

    public void SetDead(Entity<RMCPlantComponent?> plant, bool value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Dead = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.Dead));
    }

    public void SetHarvestReady(Entity<RMCPlantComponent?> plant, bool value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Harvest = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantComponent.Harvest));
    }

    public void SetIdentity(Entity<RMCPlantComponent?> plant, string name, string noun, string displayName, bool mysterious)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Name = name;
        plant.Comp.Noun = noun;
        plant.Comp.DisplayName = displayName;
        plant.Comp.Mysterious = mysterious;
        Dirty(plant.Owner, plant.Comp);
    }

    public void SetEndurance(Entity<RMCPlantGrowthComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Endurance = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantGrowthComponent.Endurance));
    }

    public void SetMaturation(Entity<RMCPlantGrowthComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Maturation = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantGrowthComponent.Maturation));
    }

    public void SetPotency(Entity<RMCPlantChemicalsComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Potency = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantChemicalsComponent.Potency));
    }

    public void SetPotencyCounter(Entity<RMCPlantChemicalsComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.PotencyCounter = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantChemicalsComponent.PotencyCounter));
    }

    public void SetChemAddCounter(Entity<RMCPlantChemicalsComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.ChemAddCounter = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantChemicalsComponent.ChemAddCounter));
    }

    public void SetYield(Entity<RMCPlantHarvestComponent?> plant, int value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Yield = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantHarvestComponent.Yield));
    }

    public void SetHarvestRepeat(Entity<RMCPlantHarvestComponent?> plant, HarvestType value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.HarvestRepeat = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantHarvestComponent.HarvestRepeat));
    }

    public void SetPestTolerance(Entity<RMCPlantTraitsComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.PestTolerance = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantTraitsComponent.PestTolerance));
    }

    public void SetWeedTolerance(Entity<RMCPlantTraitsComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.WeedTolerance = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantTraitsComponent.WeedTolerance));
    }

    public void SetToxinsTolerance(Entity<RMCPlantTraitsComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.ToxinsTolerance = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantTraitsComponent.ToxinsTolerance));
    }

    public void SetTraitCosmetics(Entity<RMCPlantTraitsComponent?> plant, ResPath plantRsi, string plantIconState, string? splatPrototype)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.PlantRsi = plantRsi;
        plant.Comp.PlantIconState = plantIconState;
        plant.Comp.SplatPrototype = splatPrototype;
        Dirty(plant.Owner, plant.Comp);
    }

    public void SetLifespan(Entity<RMCPlantGrowthComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Lifespan = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantGrowthComponent.Lifespan));
    }

    public void SetProduction(Entity<RMCPlantGrowthComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Production = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantGrowthComponent.Production));
    }

    public void SetLastProduce(Entity<RMCPlantGrowthComponent?> plant, int value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.LastProduce = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantGrowthComponent.LastProduce));
    }

    public void SetGrowthStages(Entity<RMCPlantGrowthComponent?> plant, int value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.GrowthStages = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantGrowthComponent.GrowthStages));
    }

    public void SetSeedless(Entity<RMCPlantHarvestComponent?> plant, bool value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp) || plant.Comp.Seedless == value)
            return;

        plant.Comp.Seedless = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantHarvestComponent.Seedless));
    }

    public void SetRepeatHarvestCounter(Entity<RMCPlantHarvestComponent?> plant, float value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.RepeatHarvestCounter = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantHarvestComponent.RepeatHarvestCounter));
    }

    public void SetHarvestProducts(Entity<RMCPlantHarvestComponent?> plant, string packetPrototype, List<string> productPrototypes)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.PacketPrototype = packetPrototype;
        plant.Comp.ProductPrototypes = productPrototypes;
        Dirty(plant.Owner, plant.Comp);
    }

    public void SetMutations(Entity<RMCPlantMutationComponent?> plant, List<RandomPlantMutation> value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.Mutations = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantMutationComponent.Mutations));
    }

    public void SetMutationPrototypes(Entity<RMCPlantMutationComponent?> plant, List<string> value)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.MutationPrototypes = value;
        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantMutationComponent.MutationPrototypes));
    }

    public void DirtyMutationSlots(Entity<RMCPlantMutationComponent?> plant)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantMutationComponent.Slots));
    }
    public void DirtyChemicals(Entity<RMCPlantChemicalsComponent?> plant)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        DirtyField(plant.Owner, plant.Comp, nameof(RMCPlantChemicalsComponent.Chemicals));
    }

    public void SetAtmosphericTolerance(Entity<RMCPlantAtmosphericComponent?> plant, float minHeat, float maxHeat, float minPressure, float maxPressure)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.MinHeat = minHeat;
        plant.Comp.MaxHeat = maxHeat;
        plant.Comp.MinPressure = minPressure;
        plant.Comp.MaxPressure = maxPressure;
        Dirty(plant.Owner, plant.Comp);
    }

    public void SetLightTolerance(Entity<RMCPlantAtmosphericComponent?> plant, float idealLight, float lightTolerance)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.IdealLight = idealLight;
        plant.Comp.LightTolerance = lightTolerance;
        Dirty(plant.Owner, plant.Comp);
    }

    public void SetMetabolismRates(Entity<RMCPlantMetabolismComponent?> plant, float nutrientConsumption, float waterConsumption)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        plant.Comp.NutrientConsumption = nutrientConsumption;
        plant.Comp.WaterConsumption = waterConsumption;
        Dirty(plant.Owner, plant.Comp);
    }

    public void DirtyGasses(Entity<RMCConsumeExudeGasComponent?> plant)
    {
        if (!Resolve(plant.Owner, ref plant.Comp))
            return;

        Dirty(plant.Owner, plant.Comp);
    }

    public void AdjustPestLevel(Entity<RMCPlantTrayComponent?> tray, float amount)
    {
        if (!Resolve(tray.Owner, ref tray.Comp))
            return;

        tray.Comp.PestLevel += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.PestLevel));
    }

    public void SetPestLevel(Entity<RMCPlantTrayComponent?> tray, float value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.PestLevel == value)
            return;

        tray.Comp.PestLevel = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.PestLevel));
    }

    public void AdjustNutritionLevel(Entity<RMCPlantTrayComponent?> tray, float amount)
    {
        if (!Resolve(tray.Owner, ref tray.Comp))
            return;

        tray.Comp.NutritionLevel += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.NutritionLevel));
    }

    public void AdjustWaterLevel(Entity<RMCPlantTrayComponent?> tray, float amount)
    {
        if (!Resolve(tray.Owner, ref tray.Comp))
            return;

        tray.Comp.WaterLevel += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.WaterLevel));
    }

    public void AdjustWeedLevel(Entity<RMCPlantTrayComponent?> tray, float amount)
    {
        if (!Resolve(tray.Owner, ref tray.Comp))
            return;

        tray.Comp.WeedLevel += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.WeedLevel));
    }

    public void AdjustToxins(Entity<RMCPlantTrayComponent?> tray, float amount)
    {
        if (!Resolve(tray.Owner, ref tray.Comp))
            return;

        tray.Comp.Toxins += amount;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.Toxins));
    }

    public void SetMissingGas(Entity<RMCPlantTrayComponent?> tray, int value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.MissingGas == value)
            return;

        tray.Comp.MissingGas = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.MissingGas));
    }

    public void SetImproperHeat(Entity<RMCPlantTrayComponent?> tray, bool value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.ImproperHeat == value)
            return;

        tray.Comp.ImproperHeat = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.ImproperHeat));
    }

    public void SetImproperPressure(Entity<RMCPlantTrayComponent?> tray, bool value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.ImproperPressure == value)
            return;

        tray.Comp.ImproperPressure = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.ImproperPressure));
    }

    public void SetNutritionLevel(Entity<RMCPlantTrayComponent?> tray, float value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.NutritionLevel == value)
            return;

        tray.Comp.NutritionLevel = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.NutritionLevel));
    }

    public void SetWaterLevel(Entity<RMCPlantTrayComponent?> tray, float value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.WaterLevel == value)
            return;

        tray.Comp.WaterLevel = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.WaterLevel));
    }

    public void SetWeedLevel(Entity<RMCPlantTrayComponent?> tray, float value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.WeedLevel == value)
            return;

        tray.Comp.WeedLevel = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.WeedLevel));
    }

    public void SetToxins(Entity<RMCPlantTrayComponent?> tray, float value)
    {
        if (!Resolve(tray.Owner, ref tray.Comp) || tray.Comp.Toxins == value)
            return;

        tray.Comp.Toxins = value;
        DirtyField(tray.Owner, tray.Comp, nameof(RMCPlantTrayComponent.Toxins));
    }

    public void CheckLevelSanity(EntityUid trayUid, RMCPlantTrayComponent tray)
    {
        SetNutritionLevel((trayUid, tray), MathHelper.Clamp(tray.NutritionLevel, 0f, 10f));
        SetWaterLevel((trayUid, tray), MathHelper.Clamp(tray.WaterLevel, 0f, 100f));
        SetPestLevel((trayUid, tray), MathHelper.Clamp(tray.PestLevel, 0f, 10f));
        SetWeedLevel((trayUid, tray), MathHelper.Clamp(tray.WeedLevel, 0f, 10f));
        SetToxins((trayUid, tray), MathHelper.Clamp(tray.Toxins, 0f, 10f));
    }

    public void RemovePlant(EntityUid trayUid, RMCPlantTrayComponent tray)
    {
        if (tray.PlantSlot.ContainedEntity is { } plant)
        {
            _container.Remove(plant, tray.PlantSlot);
            PredictedQueueDel(plant);
        }

        SetPestLevel((trayUid, tray), 0);
        SetImproperPressure((trayUid, tray), false);
        SetImproperHeat((trayUid, tray), false);

        UpdateSprite(trayUid, tray);
    }

    public void UpdateSprite(EntityUid trayUid, RMCPlantTrayComponent tray)
    {
        tray.UpdateSpriteAfterUpdate = false;

        var plant = tray.PlantSlot.ContainedEntity;
        if (plant != null)
            Comp<RMCPlantComponent>(plant.Value).UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(trayUid, out var app))
            return;

        if (plant != null)
        {
            var comp = Comp<RMCPlantComponent>(plant.Value);
            var growth = Comp<RMCPlantGrowthComponent>(plant.Value);

            if (tray.DrawWarnings)
                _appearance.SetData(trayUid, RMCPlantTrayVisuals.HealthLight, comp.Health <= growth.Endurance / 2f, app);

            var bioluminescent = CompOrNull<RMCPlantTraitBioluminescentComponent>(plant.Value);
            UpdateBioluminescence(plant.Value, bioluminescent != null && !comp.Dead, bioluminescent);
        }
        else
        {
            _appearance.SetData(trayUid, RMCPlantTrayVisuals.HealthLight, false, app);
        }

        if (!tray.DrawWarnings)
            return;

        _appearance.SetData(trayUid, RMCPlantTrayVisuals.WaterLight, tray.WaterLevel <= 10, app);
        _appearance.SetData(trayUid, RMCPlantTrayVisuals.NutritionLight, tray.NutritionLevel <= 2, app);
        _appearance.SetData(trayUid, RMCPlantTrayVisuals.AlertLight,
            tray.WeedLevel >= 5 || tray.PestLevel >= 5 || tray.Toxins >= 5 || tray.ImproperHeat ||
            tray.ImproperPressure || tray.MissingGas > 0, app);
        _appearance.SetData(trayUid, RMCPlantTrayVisuals.HarvestLight,
            plant != null && Comp<RMCPlantComponent>(plant.Value).Harvest, app);
    }

    private void UpdateBioluminescence(EntityUid plant, bool enabled, RMCPlantTraitBioluminescentComponent? bioluminescent)
    {
        if (enabled && bioluminescent != null)
        {
            var light = _pointLight.EnsureLight(plant);
            _pointLight.SetColor(plant, bioluminescent.Color, light);
            _pointLight.SetRadius(plant, bioluminescent.Radius, light);
            _pointLight.SetEnergy(plant, 1f, light);
            _pointLight.SetEnabled(plant, true, light);
        }
        else if (_pointLight.TryGetLight(plant, out var light))
        {
            _pointLight.SetEnabled(plant, false, light);
        }
    }
}
