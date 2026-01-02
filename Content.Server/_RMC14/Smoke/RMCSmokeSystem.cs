using Content.Server.Spreader;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Smoke;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Coordinates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;

namespace Content.Server._RMC14.Smoke;

public sealed class RMCSmokeSystem : SharedRMCSmokeSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private readonly List<(MapGridComponent Grid, TileRef Tile)> _tiles = new();

    private EntityQuery<EvenSmokeComponent> _evenSmokeQuery;
    private EntityQuery<TimedDespawnComponent> _timedDespawnQuery;

    public override void Initialize()
    {
        base.Initialize();

        _evenSmokeQuery = GetEntityQuery<EvenSmokeComponent>();
        _timedDespawnQuery = GetEntityQuery<TimedDespawnComponent>();

        SubscribeLocalEvent<EvenSmokeComponent, SpreadNeighborsEvent>(OnEvenSmokeSpreadNeighbors);
    }

    private void OnEvenSmokeSpreadNeighbors(Entity<EvenSmokeComponent> ent, ref SpreadNeighborsEvent args)
    {
        if (ent.Comp.Range == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        _tiles.Clear();
        _tiles.AddRange(args.NeighborFreeTiles);
        foreach (var neighbor in args.Neighbors)
        {
            if (!_rmcMap.TryGetTileRefForEnt(neighbor.ToCoordinates(), out var grid, out var tile))
                continue;

            var neighborTile = (grid.Comp, tile);
            if (!_tiles.Contains(neighborTile))
                _tiles.Add(neighborTile);
        }

        foreach (var tile in _tiles)
        {
            var coords = _map.GridTileToLocal(tile.Tile.GridUid, tile.Grid, tile.Tile.GridIndices);

            var smokeEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator(coords);
            var blockSmoke = false;
            while (smokeEnumerator.MoveNext(out var uid))
            {
                if (TryComp<EvenSmokeComponent>(uid, out var evenSmoke) && evenSmoke.Spawn == ent.Comp.Spawn)
                {
                    blockSmoke = true;
                    break;
                }
            }

            if (blockSmoke)
                continue;

            var smoke = SpawnAtPosition(ent.Comp.Spawn, coords);
            _hive.SetSameHive(ent.Owner, smoke);
            if (_evenSmokeQuery.TryComp(smoke, out var smokeComp))
                smokeComp.Range = ent.Comp.Range - 1;

            if (_timedDespawnQuery.TryComp(ent, out var selfDespawn) &&
                _timedDespawnQuery.TryComp(smoke, out var otherDespawn))
            {
                otherDespawn.Lifetime = selfDespawn.Lifetime;
            }

            EnsureComp<ActiveEdgeSpreaderComponent>(smoke);
        }
    }
}
