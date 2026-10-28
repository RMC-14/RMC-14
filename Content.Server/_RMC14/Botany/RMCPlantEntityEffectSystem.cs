using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Botany;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.PlantMetabolism;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantEntityEffectSystem : EntitySystem
{
    [Dependency] private readonly RMCPlantGrowthSystem _plantGrowth = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantMutationController>>(OnExecutePlantMutationController);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustHealth>>(OnExecutePlantAdjustHealth);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustMutationLevel>>(OnExecutePlantAdjustMutationLevel);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustMutationMod>>(OnExecutePlantAdjustMutationMod);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustNutrition>>(OnExecutePlantAdjustNutrition);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustPests>>(OnExecutePlantAdjustPests);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustPotency>>(OnExecutePlantAdjustPotency);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustToxins>>(OnExecutePlantAdjustToxins);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustWater>>(OnExecutePlantAdjustWater);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustWeeds>>(OnExecutePlantAdjustWeeds);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAffectGrowth>>(OnExecutePlantAffectGrowth);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantCryoxadone>>(OnExecutePlantCryoxadone);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantDestroySeeds>>(OnExecutePlantDestroySeeds);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantDiethylamine>>(OnExecutePlantDiethylamine);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantPhalanximine>>(OnExecutePlantPhalanximine);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantRestoreSeeds>>(OnExecutePlantRestoreSeeds);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<RobustHarvest>>(OnExecuteRobustHarvest);
    }

    private bool CanMetabolizePlant(EntityUid plant, [NotNullWhen(true)] out RMCPlantComponent? plantComponent,
        bool mustHaveAlivePlant = true, bool mustHaveMutableSeed = false)
    {
        if (!TryComp(plant, out plantComponent))
            return false;

        if (mustHaveAlivePlant && plantComponent.Dead)
            return false;

        if (mustHaveMutableSeed && (!TryComp(plant, out RMCPlantMutationComponent? mutation) || mutation.MutationTier == RMCPlantMutationTier.Immutable))
            return false;

        return true;
    }

    private bool TryGetTray(RMCPlantComponent plantComponent, out EntityUid trayUid, [NotNullWhen(true)] out RMCPlantTrayComponent? tray)
    {
        tray = null;
        trayUid = default;

        if (plantComponent.Tray is not { } uid || !TryComp(uid, out tray))
            return false;

        trayUid = uid;
        return true;
    }

    private static float PerUnit(EntityEffectBaseArgs args, float amount)
    {
        return args is EntityEffectReagentArgs reagentArgs ? amount * (float) reagentArgs.Quantity : amount;
    }

    private void OnExecutePlantAdjustHealth(ref ExecuteEntityEffectEvent<PlantAdjustHealth> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        _plantTray.AdjustHealth((args.Args.TargetEntity, plantComp, null), PerUnit(args.Args, args.Effect.Amount));

        if (TryGetTray(plantComp, out var trayUid, out var tray))
            _plantTray.CheckHealth(trayUid, tray, args.Args.TargetEntity);
    }

    private void OnExecutePlantAdjustMutationLevel(ref ExecuteEntityEffectEvent<PlantAdjustMutationLevel> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        _plantTray.AdjustMutationLevel((args.Args.TargetEntity, plantComp), PerUnit(args.Args, args.Effect.Amount) * plantComp.MutationMod);
    }

    private void OnExecutePlantAdjustMutationMod(ref ExecuteEntityEffectEvent<PlantAdjustMutationMod> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        _plantTray.AdjustMutationMod((args.Args.TargetEntity, plantComp), PerUnit(args.Args, args.Effect.Amount));
    }

    private void OnExecutePlantAdjustNutrition(ref ExecuteEntityEffectEvent<PlantAdjustNutrition> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp, mustHaveAlivePlant: false))
            return;

        if (TryGetTray(plantComp, out var trayUid, out var tray))
            _plantTray.AdjustNutritionLevel((trayUid, tray), PerUnit(args.Args, args.Effect.Amount));
    }

    private void OnExecutePlantAdjustPests(ref ExecuteEntityEffectEvent<PlantAdjustPests> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        if (TryGetTray(plantComp, out var trayUid, out var tray))
            _plantTray.AdjustPestLevel((trayUid, tray), PerUnit(args.Args, args.Effect.Amount));
    }

    private void OnExecutePlantAdjustPotency(ref ExecuteEntityEffectEvent<PlantAdjustPotency> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantChemicalsComponent? chemicals))
            return;

        _plantTray.SetPotency((args.Args.TargetEntity, chemicals), Math.Max(chemicals.Potency + PerUnit(args.Args, args.Effect.Amount), 1));
    }

    private void OnExecutePlantAdjustToxins(ref ExecuteEntityEffectEvent<PlantAdjustToxins> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        if (TryGetTray(plantComp, out var trayUid, out var tray))
            _plantTray.AdjustToxins((trayUid, tray), PerUnit(args.Args, args.Effect.Amount));
    }

    private void OnExecutePlantAdjustWater(ref ExecuteEntityEffectEvent<PlantAdjustWater> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp, mustHaveAlivePlant: false))
            return;

        if (TryGetTray(plantComp, out var trayUid, out var tray))
            _plantTray.AdjustWaterLevel((trayUid, tray), PerUnit(args.Args, args.Effect.Amount));
    }

    private void OnExecutePlantAdjustWeeds(ref ExecuteEntityEffectEvent<PlantAdjustWeeds> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        if (TryGetTray(plantComp, out var trayUid, out var tray))
            _plantTray.AdjustWeedLevel((trayUid, tray), PerUnit(args.Args, args.Effect.Amount));
    }

    private void OnExecutePlantAffectGrowth(ref ExecuteEntityEffectEvent<PlantAffectGrowth> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantGrowthComponent? growth))
            return;

        _plantGrowth.AffectGrowth((args.Args.TargetEntity, plantComp, growth), (int) args.Effect.Amount);
    }

    private void OnExecutePlantCryoxadone(ref ExecuteEntityEffectEvent<PlantCryoxadone> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantComp))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantGrowthComponent? growth))
            return;

        var deviation = plantComp.Age > growth.Maturation
            ? (int) Math.Max(growth.Maturation - 1, plantComp.Age - _random.Next(7, 10))
            : (int) (growth.Maturation / growth.GrowthStages);

        _plantTray.AdjustAge((args.Args.TargetEntity, plantComp), -deviation);
        _plantTray.SetLastProduce((args.Args.TargetEntity, growth), plantComp.Age);
        _plantTray.AdjustSkipAging((args.Args.TargetEntity, plantComp), 1);

        if (TryGetTray(plantComp, out _, out var tray))
            tray.ForceUpdate = true;
    }

    private void OnExecutePlantDestroySeeds(ref ExecuteEntityEffectEvent<PlantDestroySeeds> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _, mustHaveMutableSeed: true))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantHarvestComponent? harvest) || harvest.Seedless)
            return;

        _popup.PopupEntity(Loc.GetString("botany-plant-seedsdestroyed"), args.Args.TargetEntity, PopupType.SmallCaution);
        _plantTray.SetSeedless((args.Args.TargetEntity, harvest), true);
    }

    private void OnExecutePlantDiethylamine(ref ExecuteEntityEffectEvent<PlantDiethylamine> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _, mustHaveMutableSeed: true))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantGrowthComponent? growth))
            return;

        if (_random.Prob(0.1f))
            _plantTray.SetLifespan((args.Args.TargetEntity, growth), growth.Lifespan + 1);

        if (_random.Prob(0.1f))
            _plantTray.SetEndurance((args.Args.TargetEntity, growth), growth.Endurance + 1);
    }

    private void OnExecutePlantPhalanximine(ref ExecuteEntityEffectEvent<PlantPhalanximine> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _, mustHaveMutableSeed: true))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantGrowthComponent? growth))
            return;

        growth.Viable = true;
        DirtyField(args.Args.TargetEntity, growth, nameof(RMCPlantGrowthComponent.Viable));
    }

    private void OnExecutePlantRestoreSeeds(ref ExecuteEntityEffectEvent<PlantRestoreSeeds> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _, mustHaveMutableSeed: true))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantHarvestComponent? harvest) || !harvest.Seedless)
            return;

        _popup.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.Args.TargetEntity);
        _plantTray.SetSeedless((args.Args.TargetEntity, harvest), false);
    }

    private void OnExecuteRobustHarvest(ref ExecuteEntityEffectEvent<RobustHarvest> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantChemicalsComponent? chemicals) ||
            !TryComp(args.Args.TargetEntity, out RMCPlantHarvestComponent? harvest))
        {
            return;
        }

        if (chemicals.Potency < args.Effect.PotencyLimit)
        {
            _plantTray.SetPotency((args.Args.TargetEntity, chemicals),
                Math.Min(chemicals.Potency + args.Effect.PotencyIncrease, args.Effect.PotencyLimit));

            if (chemicals.Potency > args.Effect.PotencySeedlessThreshold)
                _plantTray.SetSeedless((args.Args.TargetEntity, harvest), true);
        }
        else if (harvest.Yield > 1 && _random.Prob(0.1f))
        {
            // Too much of a good thing reduces yield
            _plantTray.SetYield((args.Args.TargetEntity, harvest), harvest.Yield - 1);
        }
    }

    private void OnExecutePlantMutationController(ref ExecuteEntityEffectEvent<PlantMutationController> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out _))
            return;

        if (!TryComp(args.Args.TargetEntity, out RMCPlantMutationComponent? mutation))
            return;

        var controller = mutation.Slots;
        foreach (var slot in args.Effect.Slots)
        {
            var current = controller.GetValueOrDefault(slot, 0f);
            if (args.Effect.Enable)
            {
                if (current < args.Effect.EnableValue)
                    controller[slot] = args.Effect.EnableValue;
            }
            else if (current > args.Effect.SuppressValue)
            {
                controller[slot] = args.Effect.SuppressValue;
            }
        }

        _plantTray.DirtyMutationSlots((args.Args.TargetEntity, mutation));
    }
}
