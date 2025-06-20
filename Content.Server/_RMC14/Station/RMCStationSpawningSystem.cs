using Content.Server.Station.Systems;
using Content.Shared._RMC14.Station;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Station;

public sealed class RMCStationSpawningSystem : SharedRMCStationSpawningSystem
{
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override EntityUid? SpawnPlayerMob(EntityCoordinates coordinates,
        ProtoId<JobPrototype>? job,
        HumanoidCharacterProfile? profile,
        EntityUid? station,
        EntityUid? entity = null)
    {
        return _stationSpawning.SpawnPlayerMob(coordinates, job, profile, station, entity);
    }
}
