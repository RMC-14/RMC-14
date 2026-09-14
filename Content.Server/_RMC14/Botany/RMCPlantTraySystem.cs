using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared._RMC14.Botany;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantTraySystem : SharedRMCPlantTraySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    [Dependency] private readonly RMCPlantGrowthSystem _plantGrowth = default!;
    [Dependency] private readonly RMCPlantMetabolismSystem _plantMetabolism = default!;
    [Dependency] private readonly RMCPlantMutationSystem _plantMutation = default!;
    [Dependency] private readonly RMCPlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly RMCPlantSampleSystem _plantSample = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantTrayComponent, InteractHandEvent>(OnInteractHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RMCPlantTrayComponent>();
        while (query.MoveNext(out var uid, out var tray))
        {
            if (tray.NextUpdate > _gameTiming.CurTime)
                continue;
            tray.NextUpdate = _gameTiming.CurTime + tray.UpdateDelay;

            Update(uid, tray);
        }
    }

    #region Interaction

    /// <summary>
    /// planting, sampling, harvesting, & composting
    /// </summary>
    protected override void InteractUsingUnhandled(Entity<RMCPlantTrayComponent> entity, ref InteractUsingEvent args)
    {
        var (uid, tray) = entity;

        if (TryComp(args.Used, out RMCPlantSeedComponent? seedComp))
        {
            if (tray.PlantSlot.ContainedEntity == null)
            {
                PlantSeed(uid, tray, args.Used, seedComp, args.User);
                args.Handled = true;
                return;
            }

            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                ("name", Comp<MetaDataComponent>(uid).EntityName)), args.User, PopupType.Medium);
            return;
        }

        if (HasComp<RMCPlantSampleTakerComponent>(args.Used))
        {
            args.Handled = true;
            _plantSample.TrySample(uid, tray, args.User);
            return;
        }

        if (HasComp<SharpComponent>(args.Used))
        {
            args.Handled = true;
            _plantHarvest.DoHarvest(uid, args.User, tray);
            return;
        }

        if (TryComp<RMCProduceComponent>(args.Used, out var produce))
        {
            args.Handled = true;
            _popup.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
                ("owner", uid),
                ("usingItem", args.Used)), args.User, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                ("user", args.User),
                ("usingItem", args.Used),
                ("owner", uid)), uid, Filter.PvsExcept(args.User), true);

            if (_solutionContainerSystem.TryGetSolution(args.Used, produce.SolutionName, out var soln2, out var solution2))
            {
                if (_solutionContainerSystem.ResolveSolution(uid, tray.SoilSolutionName, ref tray.SoilSolution, out var solution1))
                {
                    var fillAmount = FixedPoint2.Min(solution2.Volume, solution1.AvailableVolume);
                    _solutionContainerSystem.TryAddSolution(tray.SoilSolution.Value, _solutionContainerSystem.SplitSolution(soln2.Value, fillAmount));

                    ForceUpdateByExternalCause(uid, tray);
                }
            }

            if (TryComp<RMCPlantChemicalsComponent>(args.Used, out var chemicals))
            {
                var nutrientBonus = chemicals.Potency / 2.5f;
                _plantMetabolism.AdjustNutrient((uid, tray), nutrientBonus);
            }

            QueueDel(args.Used);
        }
    }

    private void PlantSeed(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid packet, RMCPlantSeedComponent seedComp, EntityUid user)
    {
        if (seedComp.PlantPrototype == null)
            return;

        var mutation = CompOrNull<RMCPlantSeedMutationComponent>(packet);

        var plant = Spawn(seedComp.PlantPrototype.Value.Id, Transform(trayUid).Coordinates);
        _container.Insert(plant, tray.PlantSlot);

        if (mutation?.MutatedComponents != null)
            _plantSeed.ApplyMutatedComponents(plant, mutation.MutatedComponents);

        var comp = Comp<RMCPlantComponent>(plant);
        var growth = Comp<RMCPlantGrowthComponent>(plant);
        comp.Tray = trayUid;
        comp.Dead = false;
        comp.Age = 1;
        comp.Health = seedComp.HealthOverride ?? growth.Endurance;

        var name = Loc.GetString(comp.Name);
        var noun = Loc.GetString(comp.Noun);
        _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
            ("seedName", name), ("seedNoun", noun)), user, PopupType.Medium);

        if (TryComp<PaperLabelComponent>(packet, out var paperLabel))
            _itemSlots.TryEjectToHands(packet, paperLabel.LabelSlot, user);

        QueueDel(packet);

        CheckLevelSanity(trayUid, tray);
        UpdateSprite(trayUid, tray);

        if (TryComp(plant, out RMCPlantLogComponent? log) && log.PlantLogImpact != null)
            _adminLogger.Add(LogType.Botany, log.PlantLogImpact.Value,
                $"{ToPrettyString(user):player} planted {name} at Pos:{Transform(trayUid).Coordinates}.");
    }

    private void OnInteractHand(Entity<RMCPlantTrayComponent> entity, ref InteractHandEvent args)
    {
        _plantHarvest.DoHarvest(entity.Owner, args.User, entity.Comp);
    }

    #endregion

    #region Growth cycle

    public void Update(EntityUid uid, RMCPlantTrayComponent tray)
    {
        var plantUid = tray.PlantSlot.ContainedEntity;

        _plantMetabolism.UpdateReagents(uid, tray, plantUid);

        var curTime = _gameTiming.CurTime;

        if (tray.ForceUpdate)
            tray.ForceUpdate = false;
        else
        {
            var metabolismAdjust = plantUid != null ? Comp<RMCPlantComponent>(plantUid.Value).MetabolismAdjust : 0f;
            var adjustedDelay = tray.CycleDelay + TimeSpan.FromSeconds(metabolismAdjust);
            if (curTime < (tray.LastCycle + adjustedDelay))
            {
                if (tray.UpdateSpriteAfterUpdate)
                    UpdateSprite(uid, tray);
                return;
            }

            if (plantUid != null)
                _plantGrowth.DecayMetabolismAdjust((plantUid.Value, Comp<RMCPlantComponent>(plantUid.Value)));
        }

        tray.LastCycle = curTime;

        var plantComp = plantUid != null ? Comp<RMCPlantComponent>(plantUid.Value) : null;

        // Process mutations
        if (plantComp != null && plantComp.MutationLevel > 0)
        {
            _plantMutation.MutatePlant(plantUid!.Value, Math.Min(plantComp.MutationLevel, 25));
            plantComp.UpdateSpriteAfterUpdate = true;
            SetMutationLevel((plantUid.Value, plantComp), 0f);
        }

        var trayGrowEvent = new RMCPlantTrayGrowEvent(uid, plantUid);
        RaiseLocalEvent(uid, ref trayGrowEvent);

        // If we have no plant, or the plant is dead
        if (plantUid == null || plantComp!.Dead)
        {
            if (tray.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, tray);
            return;
        }

        var plant = plantUid.Value;
        var growth = Comp<RMCPlantGrowthComponent>(plant);

        // Advance age
        _plantGrowth.TickAging((plant, plantComp));

        _plantMetabolism.Tick(plant, (uid, tray));

        var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

        // Make sure genetics are viabl
        if (!growth.Viable)
        {
            _plantGrowth.AffectGrowth((plant, plantComp, growth), -1);
            AdjustHealth((plant, plantComp, growth), -6 * healthMod);
        }

        // Prevents the plant from aging when lacking resources
        if (plantComp.SkipAging < 10)
        {
            if (tray.NutritionLevel > 5)
            {
                if (_random.Prob(0.35f))
                    AdjustHealth((plant, plantComp, growth), healthMod);
            }
            else
            {
                _plantGrowth.AffectGrowth((plant, plantComp, growth), -1);
                if (_random.Prob(0.35f))
                    AdjustHealth((plant, plantComp, growth), -healthMod);
            }

            if (tray.WaterLevel > 10)
            {
                if (_random.Prob(0.35f))
                    AdjustHealth((plant, plantComp, growth), healthMod);
            }
            else
            {
                _plantGrowth.AffectGrowth((plant, plantComp, growth), -1);
                if (_random.Prob(0.35f))
                    AdjustHealth((plant, plantComp, growth), -healthMod);
            }

            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;

        var growEvent = new RMCPlantGrowEvent(plant, uid, environment, healthMod);
        RaiseLocalEvent(ref growEvent);

        if (_plantGrowth.TickLifespanAndProduction((plant, plantComp, growth)))
        {
            // Revert back to seed packet
            _plantSeed.SpawnSeedPacket(plant, Transform(uid).Coordinates, uid, null);
            RemovePlant(uid, tray);
            tray.ForceUpdate = true;
            Update(uid, tray);
            return;
        }

        CheckHealth(uid, tray, plant);

        if (TryComp(plant, out RMCPlantHarvestComponent? harvest)
            && plantComp.Harvest && harvest.HarvestRepeat == HarvestType.SelfHarvest)
        {
            _plantHarvest.AutoHarvest(uid, tray);
        }

        CheckLevelSanity(uid, tray);
        if (TryComp(plant, out RMCPlantComponent? stillPlant) && TryComp(plant, out RMCPlantGrowthComponent? stillGrowth))
            _plantGrowth.CheckLevelSanity((plant, stillPlant, stillGrowth));

        if ((TryComp(plant, out RMCPlantComponent? finalPlant) && finalPlant.UpdateSpriteAfterUpdate) || tray.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, tray);
    }

    #endregion

    #region Sanity/lifecycle helpers

    public void CheckHealth(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid plant)
    {
        if (Comp<RMCPlantComponent>(plant).Health <= 0)
            Die(trayUid, tray, plant);
    }

    /// <inheritdoc/>
    protected override void CheckHealth(EntityUid plant, RMCPlantComponent comp)
    {
        if (comp.Health > 0 || comp.Tray is not { } trayUid || !TryComp(trayUid, out RMCPlantTrayComponent? tray))
            return;

        CheckHealth(trayUid, tray, plant);
    }

    public void Die(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid plant)
    {
        var comp = Comp<RMCPlantComponent>(plant);
        SetDead((plant, comp), true);
        SetHarvestReady((plant, comp), false);
        SetMutationLevel((plant, comp), 0f);
        SetYieldMod((plant, comp), 1);
        SetMutationMod((plant, comp), 1f);
        SetImproperHeat((trayUid, tray), false);
        SetImproperPressure((trayUid, tray), false);
        AdjustWeedLevel((trayUid, tray), 1 * HydroponicsSpeedMultiplier);
        SetPestLevel((trayUid, tray), 0);
        UpdateSprite(trayUid, tray);
    }

    public void ForceUpdateByExternalCause(EntityUid trayUid, RMCPlantTrayComponent tray)
    {
        if (tray.PlantSlot.ContainedEntity is { } plant)
            AdjustSkipAging((plant, Comp<RMCPlantComponent>(plant)), 1);

        tray.ForceUpdate = true;
        Update(trayUid, tray);
    }

    #endregion
}
