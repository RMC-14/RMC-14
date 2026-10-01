using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.FaxResponder;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RMC14.FaxResponder;

public sealed class FaxResponderSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Run before the default SpawnPointSystem so we can handle fax responder spawns
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(SpawnPointSystem) });
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        if (args.Job == null)
            return;

        // Find matching fax responder spawn point
        var query = EntityQueryEnumerator<FaxResponderSpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (comp.Job == args.Job)
                possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count == 0)
            return;

        var spawnLoc = _random.Pick(possiblePositions);
        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }
}
