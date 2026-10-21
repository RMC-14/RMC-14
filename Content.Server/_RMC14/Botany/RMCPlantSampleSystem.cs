using Content.Server.Popups;
using Content.Shared._RMC14.Botany;
using Content.Shared.Random;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantSampleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly RMCPlantScreamSystem _plantScream = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;

    public bool TrySample(EntityUid trayUid, RMCPlantTrayComponent tray, EntityUid user)
    {
        if (tray.PlantSlot.ContainedEntity is not { } plant)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"), user);
            return false;
        }

        var comp = Comp<RMCPlantComponent>(plant);

        if (comp.Sampled)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), user);
            return false;
        }

        if (comp.Dead)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), user);
            return false;
        }

        var growth = Comp<RMCPlantGrowthComponent>(plant);
        _plantTray.AdjustHealth((plant, comp, growth), -(_random.Next(3, 6) * 10));

        var packet = _plantSeed.SpawnSeedPacket(plant, Transform(user).Coordinates, user, null);
        _randomHelper.RandomOffset(packet, 0.25f);
        var displayName = Loc.GetString(comp.DisplayName);
        _popup.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
            ("seedName", displayName)), user);

        _plantScream.DoScream(trayUid, plant);

        if (_random.Prob(0.3f))
            _plantTray.SetSampled((plant, comp));

        _plantTray.CheckLevelSanity(trayUid, tray);
        _plantTray.ForceUpdateByExternalCause(trayUid, tray);

        return true;
    }
}
