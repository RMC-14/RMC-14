using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

public sealed class RMCAdminSystem : SharedRMCAdminSystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void OpenBui(ICommonSession player, EntityUid target)
    {
        if (!CanUse(player))
            return;

        _eui.OpenEui(new RMCAdminEui(target), player);
    }

    public EntityUid RandomizeMarine(EntityUid entity,
        ProtoId<SpeciesPrototype>? species = null,
        ProtoId<StartingGearPrototype>? gear = null,
        ProtoId<JobPrototype>? job = null)
    {
        var profile = species == null
            ? HumanoidCharacterProfile.Random()
            : HumanoidCharacterProfile.RandomWithSpecies(species);
        var coordinates = _transform.GetMoverCoordinates(entity);
        var jobComp = job == null ? null : new JobComponent { Prototype = job.Value };
        var humanoid = _stationSpawning.SpawnPlayerMob(coordinates, jobComp, profile, null);

        if (gear != null)
        {
            var startingGear = _prototypes.Index<StartingGearPrototype>(gear);
            _stationSpawning.EquipStartingGear(humanoid, startingGear);
        }

        return humanoid;
    }
}
