using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.Botany;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Manual & automatic harvest, & the repeat/despawn decision afterward
/// </summary>
public sealed class RMCPlantHarvestSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly RMCPlantProduceSystem _plantProduce = default!;
    [Dependency] private readonly RMCPlantScreamSystem _plantScream = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;

    public bool DoHarvest(EntityUid trayUid, EntityUid user, RMCPlantTrayComponent tray)
    {
        if (tray.PlantSlot.ContainedEntity is not { } plant || Deleted(user))
            return false;

        var comp = Comp<RMCPlantComponent>(plant);

        if (comp.Harvest && !comp.Dead)
        {
            if (_hands.TryGetActiveItem(user, out var activeItem))
            {
                if (!_plantSeed.CanHarvest(plant, activeItem))
                {
                    _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), user);
                    return false;
                }
            }
            else if (!_plantSeed.CanHarvest(plant))
            {
                return false;
            }

            _plantProduce.Harvest(plant, user, comp.YieldMod);
            AfterHarvest(trayUid, tray, plant);
            return true;
        }

        if (!comp.Dead)
            return false;

        _plantTray.RemovePlant(trayUid, tray);
        _plantTray.CheckLevelSanity(trayUid, tray);
        return true;
    }

    public void AutoHarvest(EntityUid trayUid, RMCPlantTrayComponent tray)
    {
        if (tray.PlantSlot.ContainedEntity is not { } plant)
            return;

        var comp = Comp<RMCPlantComponent>(plant);
        if (!comp.Harvest)
            return;

        _plantProduce.AutoHarvest(plant, Transform(trayUid).Coordinates, comp.YieldMod);
        AfterHarvest(trayUid, tray, plant);
    }

    private void AfterHarvest(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid plant)
    {
        if (!TryComp(plant, out RMCPlantComponent? comp) || !TryComp(plant, out RMCPlantGrowthComponent? growth))
        {
            _plantTray.CheckLevelSanity(trayUid, tray);
            _plantTray.UpdateSprite(trayUid, tray);
            return;
        }

        _plantTray.SetHarvestReady((plant, comp), false);
        _plantTray.SetLastProduce((plant, growth), comp.Age);

        _plantScream.DoScream(trayUid, plant);

        if (TryComp(plant, out RMCPlantHarvestComponent? harvest) && harvest.HarvestRepeat == HarvestType.NoRepeat)
            _plantTray.RemovePlant(trayUid, tray);

        _plantTray.CheckLevelSanity(trayUid, tray);
        _plantTray.UpdateSprite(trayUid, tray);
    }
}
