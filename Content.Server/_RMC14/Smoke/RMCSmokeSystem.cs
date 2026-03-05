using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Server.Spreader;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Smoke;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server._RMC14.Smoke;

public sealed class RMCSmokeSystem : SharedRMCSmokeSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
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
        SubscribeLocalEvent<EvenSmokeComponent, MapInitEvent>(OnEvenSmokeMapInit);
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

            if (ent.Comp.InitialSpread > 0)
            {
                var childSmokeComponent = EnsureComp<EvenSmokeComponent>(smoke);
                childSmokeComponent.Range = ent.Comp.Range - 1;
                childSmokeComponent.Spawn = ent.Comp.Spawn;
            }
        }
    }

    private void OnEvenSmokeMapInit(Entity<EvenSmokeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.InitialSpread <= 0)
            return;

        if (_prototype.TryIndex(ent.Comp.Spawn, out var spawnProto) && spawnProto.HasComponent<EvenSmokeComponent>())
        {
            Debug.Assert(!spawnProto.HasComponent<EvenSmokeComponent>()); // This would cause an infinite loop, so we return.
            return;
        }

        SpreadBurst(ent);
    }

    private void SpreadBurst(Entity<EvenSmokeComponent> parent)
    {
        if (!_rmcMap.TryGetTileRefForEnt(parent.Owner.ToCoordinates(), out var grid, out var tile))
            return;

        var generations = parent.Comp.InitialSpread;
        var frontier = new List<(MapGridComponent Grid, TileRef Tile)>();
        frontier.Add((grid.Comp, tile));

        for (var gen = 1; gen <= generations; gen++)
        {
            var nextFrontier = new List<(MapGridComponent Grid, TileRef Tile)>();
            foreach (var (currGrid, currTile) in frontier)
            {
                var anchoredEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator((currTile.GridUid, currGrid), currTile.GridIndices);
                var blockedDirs = AtmosDirection.Invalid;
                var existingSmokes = new HashSet<Vector2i>();

                while (anchoredEnumerator.MoveNext(out var ent))
                {
                    if (TryComp<AirtightComponent>(ent, out var airtight) && airtight.AirBlocked)
                        blockedDirs |= airtight.AirBlockedDirection;

                    if (TryComp<EvenSmokeComponent>(ent, out var evenSmoke) && evenSmoke.Spawn == parent.Comp.Spawn)
                        existingSmokes.Add(currTile.GridIndices);
                }

                foreach (var direction in RMCDirectionExtensions.GetCardinals())
                {
                    if ((blockedDirs & direction.ToAtmosDirection()) != 0)
                        continue;

                    var neighborIndices = currTile.GridIndices + direction.ToIntVec();

                    if (existingSmokes.Contains(neighborIndices))
                        continue;

                    if (!_map.TryGetTileRef(currTile.GridUid, currGrid, neighborIndices, out var neighborTile))
                        continue;

                    if (neighborTile.Tile.IsEmpty)
                        continue;

                    var coords = _map.GridTileToLocal(neighborTile.GridUid, currGrid, neighborTile.GridIndices);
                    var smoke = SpawnAtPosition(parent.Comp.Spawn, coords);
                    _hive.SetSameHive(parent.Owner, smoke);

                    if (_timedDespawnQuery.TryComp(parent.Owner, out var selfDespawn) &&
                        _timedDespawnQuery.TryComp(smoke, out var otherDespawn))
                    {
                        otherDespawn.Lifetime = selfDespawn.Lifetime;
                    }

                    // Only the last generation tries to spread normally
                    if (gen == generations)
                    {
                        EnsureComp<ActiveEdgeSpreaderComponent>(smoke);
                        var smokeComp = EnsureComp<EvenSmokeComponent>(smoke);
                        smokeComp.Range = parent.Comp.Range - parent.Comp.InitialSpread;
                        smokeComp.Spawn = parent.Comp.Spawn;
                    }

                    nextFrontier.Add((currGrid, neighborTile));
                }
            }

            frontier = nextFrontier;
        }
    }
}
