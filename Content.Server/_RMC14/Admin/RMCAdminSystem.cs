using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminSystem : SharedRMCAdminSystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [ValidatePrototypeId<StartingGearPrototype>]
    private const string DefaultHumanoidGear = "CMGearRifleman";

    protected override void OpenBui(ICommonSession player, EntityUid target)
    {
        if (!CanUse(player))
            return;

        _eui.OpenEui(new RMCAdminEui(target), player);
    }

    public EntityUid RandomizeMarine(EntityUid entity, ProtoId<SpeciesPrototype>? species = null)
    {
        var profile = species == null
            ? HumanoidCharacterProfile.Random()
            : HumanoidCharacterProfile.RandomWithSpecies(species);
        var coordinates = _transform.GetMoverCoordinates(entity);
        var humanoid = _stationSpawning.SpawnPlayerMob(coordinates, null, profile, null);
        var startingGear = _prototypes.Index<StartingGearPrototype>(DefaultHumanoidGear);
        _stationSpawning.EquipStartingGear(humanoid, startingGear);
        return humanoid;
    }
}
