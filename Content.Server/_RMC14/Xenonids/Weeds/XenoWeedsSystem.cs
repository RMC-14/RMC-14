using System.Numerics;
using Content.Server.Spreader;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Xenonids.Weeds;

public sealed class XenoWeedsSystem : SharedXenoWeedsSystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private readonly List<EntityUid> _anchored = new();

    private EntityQuery<XenoNestSurfaceComponent> _xenoNestSurfaceQuery;
    private EntityQuery<XenoWeedableComponent> _xenoWeedableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _xenoNestSurfaceQuery = GetEntityQuery<XenoNestSurfaceComponent>();
        _xenoWeedableQuery = GetEntityQuery<XenoWeedableComponent>();

        SubscribeLocalEvent<XenoWeedsComponent, SpreadNeighborsEvent>(OnWeedsSpreadNeighbors);
    }

    private void OnWeedsSpreadNeighbors(Entity<XenoWeedsComponent> ent, ref SpreadNeighborsEvent args)
    {
        var source = ent.Comp.IsSource ? ent.Owner : ent.Comp.Source;
        var sourceWeeds = CompOrNull<XenoWeedsComponent>(source);

        if (source != null && sourceWeeds != null)
            Dirty(source.Value, sourceWeeds);

        // TODO RMC14
        // There is an edge case right now where existing weeds can block new weeds
        // from expanding further. If this is the case then the weeds should reassign
        // their source to this one and reactivate if it is closer to them than their
        // original source and only if it is still within range
        if (args.NeighborFreeTiles.Count <= 0 ||
            !Exists(source) ||
            !TryComp(source, out TransformComponent? transform) ||
            ent.Comp.Spawns.Id is not { } prototype)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        var any = false;
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var tileRef = neighbor.Tile;
            var gridOwner = neighbor.Grid.Owner;
            var tile = tileRef.GridIndices;

            var sourceLocal = _mapSystem.CoordinatesToTile(gridOwner, neighbor.Grid, transform.Coordinates);
            var diff = Vector2.Abs(tile - sourceLocal);
            if (diff.X >= ent.Comp.Range || diff.Y >= ent.Comp.Range)
                break;

            if (!CanPlaceWeeds((gridOwner, neighbor.Grid), tile))
                continue;

            var coords = _mapSystem.GridTileToLocal(gridOwner, neighbor.Grid, tile);
            var neighborWeeds = Spawn(prototype, coords);
            var neighborWeedsComp = EnsureComp<XenoWeedsComponent>(neighborWeeds);

            neighborWeedsComp.IsSource = false;
            neighborWeedsComp.Source = source;
            sourceWeeds?.Spread.Add(neighborWeeds);

            _hive.SetSameHive(ent.Owner, neighborWeeds);

            Dirty(neighborWeeds, neighborWeedsComp);

            EnsureComp<ActiveEdgeSpreaderComponent>(neighborWeeds);

            any = true;

            for (var i = 0; i < 4; i++)
            {
                var dir = (AtmosDirection) (1 << i);
                var pos = tile.Offset(dir);
                if (!_mapSystem.TryGetTileRef(gridOwner, neighbor.Grid, pos, out var adjacent))
                    continue;

                _anchored.Clear();
                _mapSystem.GetAnchoredEntities((gridOwner, neighbor.Grid), adjacent.GridIndices, _anchored);
                foreach (var anchored in _anchored)
                {
                    if (!_xenoWeedableQuery.TryComp(anchored, out var weedable) ||
                        weedable.Entity != null ||
                        !TryComp(anchored, out TransformComponent? weedableTransform) ||
                        !weedableTransform.Anchored)
                    {
                        continue;
                    }

                    weedable.Entity = SpawnAtPosition(weedable.Spawn, anchored.ToCoordinates());
                    if (_xenoNestSurfaceQuery.TryComp(weedable.Entity, out var surface))
                    {
                        surface.Weedable = anchored;
                        Dirty(weedable.Entity.Value, surface);
                    }

                    sourceWeeds?.Spread.Add(weedable.Entity.Value);
                }
            }
        }

        if (!any)
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
    }
}
