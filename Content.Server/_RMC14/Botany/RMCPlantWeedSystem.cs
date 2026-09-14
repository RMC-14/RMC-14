using Content.Shared._RMC14.Botany;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Weed population growth, parasite consumption, & weed tolerance dmg.
/// </summary>
public sealed class RMCPlantWeedSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    /// <summary>
    /// Wild weed type plants a tray can be overtaken by once weeds get out of hand
    /// </summary>
    private static readonly ProtoId<EntityPrototype>[] WeedInvasionPrototypes =
    [
        "RMCPlantRmcgrass",
        "RMCPlantWeeds",
        "RMCPlantHarebell",
        "RMCPlantRmcpoppy",
        "RMCPlantRmcplump",
        "RMCPlantMold",
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantTrayComponent, RMCPlantTrayGrowEvent>(OnTrayGrow);
        SubscribeLocalEvent<RMCPlantGrowEvent>(OnPlantGrow);
    }

    private void OnTrayGrow(Entity<RMCPlantTrayComponent> ent, ref RMCPlantTrayGrowEvent args)
    {
        var traits = args.Plant != null ? CompOrNull<RMCPlantTraitsComponent>(args.Plant.Value) : null;
        TickSpawn(ent, ent.Comp, args.Plant, traits);
    }

    private void OnPlantGrow(ref RMCPlantGrowEvent args)
    {
        if (!TryComp(args.Plant, out RMCPlantComponent? plantComp))
            return;

        Tick((args.Plant, plantComp), (args.Tray, Comp<RMCPlantTrayComponent>(args.Tray)), Comp<RMCPlantTraitsComponent>(args.Plant));
    }

    /// <summary>
    /// Weed spawn %, kudzu conversion, & the weed invasion roll
    /// </summary>
    public void TickSpawn(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid? plant, RMCPlantTraitsComponent? traits)
    {
        var kudzu = plant != null ? CompOrNull<RMCPlantTraitKudzuComponent>(plant.Value) : null;

        if (tray.WaterLevel > 10 && tray.NutritionLevel > 5)
        {
            var chance = plant == null ? 0.05f : kudzu != null ? 1f : 0.01f;

            if (_random.Prob(chance))
                _plantTray.AdjustWeedLevel((trayUid, tray), 1f * HydroponicsSpeedMultiplier * tray.WeedCoefficient);

            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }

        if (plant != null && kudzu != null && tray.WeedLevel >= kudzu.WeedHighLevelThreshold)
        {
            Spawn(kudzu.KudzuPrototype, Transform(trayUid).Coordinates.SnapToGrid(EntityManager));
            RemComp<RMCPlantTraitKudzuComponent>(plant.Value);

            var killedComp = Comp<RMCPlantComponent>(plant.Value);
            var killedGrowth = Comp<RMCPlantGrowthComponent>(plant.Value);
            _plantTray.AdjustHealth((plant.Value, killedComp, killedGrowth), -killedComp.Health);
        }

        // Chance for weed explosion to happen if weeds take over
        // Plants that are themselves weeds (WeedTolerance > 8) are unaffected
        if (tray.WeedLevel >= 10 && _random.Prob(0.1f))
        {
            if (plant == null || tray.WeedLevel >= traits!.WeedTolerance + 2)
                WeedInvasion(trayUid, tray, plant);
        }
    }

    public void Tick(Entity<RMCPlantComponent> plant, Entity<RMCPlantTrayComponent> tray, RMCPlantTraitsComponent traits)
    {
        if (tray.Comp.WeedLevel <= 0)
            return;

        var growth = Comp<RMCPlantGrowthComponent>(plant.Owner);

        if (HasComp<RMCPlantTraitParasiteComponent>(plant.Owner))
        {
            _plantTray.AdjustWeedLevel((tray.Owner, tray.Comp), -HydroponicsSpeedMultiplier);
            _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), HydroponicsSpeedMultiplier);
        }
        else if (tray.Comp.WeedLevel >= traits.WeedTolerance)
        {
            _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), -HydroponicsSpeedMultiplier);
        }

        if (tray.Comp.DrawWarnings)
            tray.Comp.UpdateSpriteAfterUpdate = true;
    }

    public void WeedInvasion(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid? plant)
    {
        if (plant != null)
        {
            _container.Remove(plant.Value, tray.PlantSlot);
            QueueDel(plant.Value);
        }

        var protoId = _random.Pick(WeedInvasionPrototypes);
        var newPlant = Spawn(protoId, Transform(trayUid).Coordinates);
        _container.Insert(newPlant, tray.PlantSlot);

        var comp = Comp<RMCPlantComponent>(newPlant);
        var growth = Comp<RMCPlantGrowthComponent>(newPlant);
        comp.Tray = trayUid;
        comp.Dead = false;
        comp.Age = 1;
        comp.Health = growth.Endurance;

        tray.UpdateSpriteAfterUpdate = true;
    }
}
