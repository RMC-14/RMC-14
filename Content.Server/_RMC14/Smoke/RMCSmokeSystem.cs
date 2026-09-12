using Content.Server.Atmos.Components;
using Content.Server.Spreader;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Smoke;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;

namespace Content.Server._RMC14.Smoke;

public sealed class RMCSmokeSystem : SharedRMCSmokeSystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _damageable = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;

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
        // once we've spread, do not spread anymore
        RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);

        if (ent.Comp.Range == 0)
        {
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

        if (Prototype(ent) is not { } prototype)
            return;

        var selfDespawn = _timedDespawnQuery.CompOrNull(ent);

        foreach (var tile in _tiles)
        {
            var coords = _map.GridTileToLocal(tile.Tile.GridUid, tile.Grid, tile.Tile.GridIndices);

            var smokeEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator(coords);
            var blockSmoke = false;
            while (smokeEnumerator.MoveNext(out var uid))
            {
                if (Prototype(uid)?.ID == prototype.ID && TryComp<EvenSmokeComponent>(uid, out var uidSmokeComp))
                {
                    if (uidSmokeComp.Range < ent.Comp.Range - 1)
                    {
                        // "weaker" smoke gets replaced
                        QueueDel(uid);
                    }
                    else if (uidSmokeComp.Range == ent.Comp.Range - 1
                        && selfDespawn != null
                        && _timedDespawnQuery.TryComp(uid, out var uidDespawn)
                        && uidDespawn.Lifetime < selfDespawn.Lifetime)
                    {
                        // equal smoke that doesn't last as long gets replaced
                        QueueDel(uid);
                    }
                    else
                    {
                        blockSmoke = true;
                        break;
                    }
                }
            }

            if (blockSmoke)
                continue;

            TrySpreadTo(ent, coords, out _);
        }
    }

    private void OnEvenSmokeMapInit(Entity<EvenSmokeComponent> ent, ref MapInitEvent args)
    {
        TryRefreshDamage(ent);

        if (ent.Comp.InitialSpread > 0)
            SpreadBurst(ent);
    }

    private void SpreadBurst(Entity<EvenSmokeComponent> parent)
    {
        if (!_rmcMap.TryGetTileRefForEnt(parent.Owner.ToCoordinates(), out var grid, out var tile))
            return;

        if (Prototype(parent) is not { } prototype)
            return;

        var generations = parent.Comp.InitialSpread;
        var frontier = new List<(MapGridComponent Grid, TileRef Tile, AtmosDirection FromDir)>();
        frontier.Add((grid.Comp, tile, AtmosDirection.Invalid));

        var parentDespawn = _timedDespawnQuery.CompOrNull(parent);

        for (var gen = 0; gen <= generations; gen++)
        {
            var nextFrontier = new List<(MapGridComponent Grid, TileRef Tile, AtmosDirection FromDir)>();
            foreach (var (curGrid, curTile, fromDir) in frontier)
            {
                var blocked = false;
                var anchoredEnumerator = _rmcMap.GetAnchoredEntitiesEnumerator((curTile.GridUid, curGrid), curTile.GridIndices);
                var blockedDirs = AtmosDirection.Invalid;

                // check objects that exist on our current tile
                while (anchoredEnumerator.MoveNext(out var ent))
                {
                    if (TryComp<AirtightComponent>(ent, out var airtight) && airtight.AirBlocked)
                    {
                        if ((airtight.AirBlockedDirection & fromDir) != 0)
                        {
                            // this tile blocks spreading from the direction we are trying to spread from
                            blocked = true;
                            break;
                        }

                        blockedDirs |= airtight.AirBlockedDirection;
                    }

                    if (Prototype(ent)?.ID == prototype.ID)
                    {
                        if (TryComp<EvenSmokeComponent>(ent, out var entSmokeComp)
                            && entSmokeComp.Range < parent.Comp.Range - gen)
                        {
                            // "weaker" smoke gets replaced
                            QueueDel(ent);
                        }
                        else if (parentDespawn != null
                            && entSmokeComp != null
                            && entSmokeComp.Range == parent.Comp.Range - gen
                            && _timedDespawnQuery.TryComp(ent, out var entDespawn)
                            && parentDespawn.Lifetime > entDespawn.Lifetime)
                        {
                            // same "strength" but doesn't last as long gets replaced.
                            QueueDel(ent);
                        }
                        else
                        {
                            blocked = true;
                        }
                    }
                }

                // gen 0 is the progenator and can't be blocked
                if (gen != 0 && blocked)
                    continue;

                // collect the next frontier
                foreach (var direction in RMCDirectionExtensions.GetCardinals())
                {
                    // don't try spreading into the direction we came from
                    if (direction.ToAtmosDirection() == fromDir)
                        continue;

                    // don't spread if something on our tile blocks spreading that direction
                    if ((blockedDirs & direction.ToAtmosDirection()) != 0)
                        continue;

                    var neighborIndices = curTile.GridIndices + direction.ToIntVec();

                    if (!_map.TryGetTileRef(curTile.GridUid, curGrid, neighborIndices, out var neighborTile))
                        continue;

                    if (neighborTile.Tile.IsEmpty)
                        continue;

                    nextFrontier.Add((curGrid, neighborTile, direction.ToAtmosDirection().GetOpposite()));
                }

                // gen 0 is the progenator, don't create smoke for it
                if (gen == 0)
                    continue;

                var coords = _map.GridTileToLocal(curTile.GridUid, curGrid, curTile.GridIndices);

                TrySpreadTo(parent, coords, out _, activateSpreading: gen == generations, newRange: parent.Comp.Range - gen);
            }

            frontier = nextFrontier;
        }
    }

    private bool TrySpreadTo(Entity<EvenSmokeComponent> source, EntityCoordinates coords, out EntityUid? newSmokeUid, bool activateSpreading = true, int? newRange = null)
    {
        newSmokeUid = null;
        var prototype = Prototype(source);
        if (prototype == null)
            return false;

        // To prevent infinite loops due to every spawn having the same InitialSpread, we have to make
        // the entity WITHOUT map initialization, set the InitialSpread properly, and THEN map init it.
        // Additionally, some calculations based on range are done on init, so we need to set range before
        // the entity is initialized as well.
        var newSmoke = EntityManager.CreateEntityUninitialized(prototype.ID, coords);
        EntityManager.InitializeAndStartEntity(newSmoke, false);

        _hive.SetSameHive(source.Owner, newSmoke);

        if (_timedDespawnQuery.TryComp(source.Owner, out var selfDespawn) &&
            _timedDespawnQuery.TryComp(newSmoke, out var otherDespawn))
        {
            otherDespawn.Lifetime = selfDespawn.Lifetime;
        }

        var newSmokeComp = EnsureComp<EvenSmokeComponent>(newSmoke);
        newSmokeComp.Range = newRange ?? source.Comp.Range - 1;
        newSmokeComp.InitialSpread = 0; // spreads should never burst, that is handled by the initially created entity.

        if (activateSpreading)
        {
            EnsureComp<ActiveEdgeSpreaderComponent>(newSmoke);
        }

        // Finally map-init the entity
        EntityManager.RunMapInit(newSmoke, MetaData(newSmoke));

        newSmokeUid = newSmoke;
        return true;
    }

    private bool TryRefreshDamage(Entity<EvenSmokeComponent> ent)
    {
        if (!ent.Comp.RangeMultipliesRegularDamage)
            return true;

        return _damageable.TryScaleDamageOverTimeDamage(ent.Owner, ent.Comp.Range);
    }
}
