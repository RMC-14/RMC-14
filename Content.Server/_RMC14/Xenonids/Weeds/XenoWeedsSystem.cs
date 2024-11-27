using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Spreader;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Weeds;

public sealed class XenoWeedsSystem : SharedXenoWeedsSystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private static readonly ProtoId<TagPrototype> IgnoredTag = "SpreaderIgnore";

    private readonly List<EntityUid> _anchored = new();
    private readonly List<Entity<XenoWeedsComponent>> _spread = new();

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<XenoNestSurfaceComponent> _xenoNestSurfaceQuery;
    private EntityQuery<XenoWeedableComponent> _xenoWeedableQuery;
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _xenoNestSurfaceQuery = GetEntityQuery<XenoNestSurfaceComponent>();
        _xenoWeedableQuery = GetEntityQuery<XenoWeedableComponent>();
        _xenoWeedsQuery = GetEntityQuery<XenoWeedsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _spread.Clear();

        var time = _timing.CurTime;
        var spreadingQuery = EntityQueryEnumerator<XenoWeedsSpreadingComponent, XenoWeedsComponent>();
        while (spreadingQuery.MoveNext(out var uid, out var spreading, out var weeds))
        {
            if (time < spreading.SpreadAt)
                continue;

            RemCompDeferred<XenoWeedsSpreadingComponent>(uid);
            _spread.Add((uid, weeds));
        }

        foreach (var (uid, weeds) in _spread)
        {
            if (_transform.GetGrid(uid) is not { } gridId ||
                !_mapGridQuery.TryComp(gridId, out var gridComp))
            {
                continue;
            }

            var grid = new Entity<MapGridComponent>(gridId, gridComp);
            var indices = _map.CoordinatesToTile(gridId, gridComp, uid.ToCoordinates());
            var curWeedSource = weeds.IsSource ? uid : weeds.Source;

            foreach (var cardinal in _rmcMap.AtmosCardinalDirections)
            {
                var blocked = false;
                EntityUid? weedsToReplace = null;
                var neighbor = indices.Offset(cardinal);
                var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(grid, neighbor);

                while (anchored.MoveNext(out var anchoredId))
                {
                    if (_airtightQuery.TryGetComponent(anchoredId, out var airtight) &&
                        airtight.AirBlocked &&
                        !_tag.HasTag(anchoredId, IgnoredTag))
                    {
                        blocked = true;
                        continue;
                    }

                    if (!_map.TryGetTileRef(grid, grid, neighbor, out var tileRef) ||
                        tileRef.Tile.IsEmpty ||
                        tileRef.Tile.IsSpace())
                    {
                        blocked = true;
                        continue;
                    }

                    if (_xenoWeedsQuery.TryComp(anchoredId, out var otherWeeds))
                    {
                        if (otherWeeds.Level > weeds.Level)
                        {
                            blocked = true;
                        }
                        else if (otherWeeds.Level == weeds.Level)
                        {
                            var otherWeedsSource = otherWeeds.IsSource ? anchoredId : otherWeeds.Source;
                            if (otherWeedsSource is null || curWeedSource is null ||
                                !_physics.TryGetDistance(anchoredId, otherWeedsSource.Value, out var distanceToOtherSource) ||
                                !_physics.TryGetDistance(anchoredId, curWeedSource.Value, out var distanceToCurSource))
                            {
                                blocked = true;
                                continue;
                            }

                            if (distanceToCurSource >= distanceToOtherSource)
                            {
                                blocked = true;
                                continue;
                            }

                            weedsToReplace = anchoredId;
                            continue;
                        }
                        else
                        {
                            weedsToReplace = anchoredId;
                            continue;
                        }
                    }
                }

                if (blocked)
                    continue;

                var source = weeds.IsSource ? uid : weeds.Source;
                var sourceWeeds = CompOrNull<XenoWeedsComponent>(source);

                if (source != null && sourceWeeds != null)
                    Dirty(source.Value, sourceWeeds);

                if (!Exists(source) ||
                    !TryComp(source, out TransformComponent? transform) ||
                    weeds.Spawns.Id is not { } prototype)
                {
                    continue;
                }

                var sourceLocal = _map.CoordinatesToTile(grid, gridComp, transform.Coordinates);
                var diff = Vector2.Abs(neighbor - sourceLocal);
                if (diff.X >= weeds.Range || diff.Y >= weeds.Range)
                    break;

                if (!CanPlaceWeedsPopup(grid, neighbor, null, weeds.SpreadsOnSemiWeedable))
                    continue;

                if (weedsToReplace != null)
                    QueueDel(weedsToReplace.Value);

                var coords = _map.GridTileToLocal(grid, grid, neighbor);
                var neighborWeeds = Spawn(prototype, coords);
                var neighborWeedsComp = EnsureComp<XenoWeedsComponent>(neighborWeeds);

                neighborWeedsComp.IsSource = false;
                neighborWeedsComp.Source = source;
                sourceWeeds?.Spread.Add(neighborWeeds);

                _hive.SetSameHive(uid, neighborWeeds);

                Dirty(neighborWeeds, neighborWeedsComp);

                EnsureComp<ActiveEdgeSpreaderComponent>(neighborWeeds);

                for (var i = 0; i < 4; i++)
                {
                    var dir = (AtmosDirection)(1 << i);
                    var pos = neighbor.Offset(dir);
                    if (!_map.TryGetTileRef(grid, grid, pos, out var adjacent))
                        continue;

                    _anchored.Clear();
                    _map.GetAnchoredEntities(grid, adjacent.GridIndices, _anchored);
                    foreach (var anchoredId in _anchored)
                    {
                        if (!_xenoWeedableQuery.TryComp(anchoredId, out var weedable) ||
                            weedable.Entity != null ||
                            !TryComp(anchoredId, out TransformComponent? weedableTransform) ||
                            !weedableTransform.Anchored)
                        {
                            continue;
                        }

                        weedable.Entity = SpawnAtPosition(weedable.Spawn, anchoredId.ToCoordinates());
                        if (_xenoNestSurfaceQuery.TryComp(weedable.Entity, out var surface))
                        {
                            surface.Weedable = anchoredId;
                            Dirty(weedable.Entity.Value, surface);
                        }

                        sourceWeeds?.Spread.Add(weedable.Entity.Value);
                    }
                }
            }
        }
    }
}
