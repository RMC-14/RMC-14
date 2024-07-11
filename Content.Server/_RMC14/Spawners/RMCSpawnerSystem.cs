using Content.Server.GameTicking;
using Content.Shared.Coordinates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Spawners;

public sealed class RMCSpawnerSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntProtoId, List<Entity<ProportionalSpawnerComponent>>> _spawners = new();

    public override void Update(float frameTime)
    {
        _spawners.Clear();

        var query = EntityQueryEnumerator<ProportionalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            _spawners.GetOrNew(comp.Id).Add((uid, comp));
        }

        if (_spawners.Count == 0)
            return;

        var players = _gameTicker.PlayersJoinedRoundNormally;
        foreach (var spawners in _spawners.Values)
        {
            _random.Shuffle(spawners);

            var spawned = 0;
            foreach (var spawner in spawners)
            {
                var coordinates = _transform.ToMapCoordinates(spawner.Owner.ToCoordinates());
                QueueDel(spawner);

                var max = Math.Max(1, players / spawner.Comp.Ratio);
                if (max <= spawned)
                    continue;

                foreach (var spawn in spawner.Comp.Prototypes)
                {
                    Spawn(spawn, coordinates);
                }

                spawned++;
            }
        }
    }
}
