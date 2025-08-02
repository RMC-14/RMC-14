using System.Linq;
using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Spreader;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Weeds;

public sealed class XenoWeedsSystem : SharedXenoWeedsSystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    private static readonly ProtoId<TagPrototype> IgnoredTag = "SpreaderIgnore";

    private readonly List<EntityUid> _anchored = new();
    private readonly List<Entity<XenoWeedsComponent>> _spread = new();

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<AllowWeedSpreadComponent> _allowWeedSpreadQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<XenoNestSurfaceComponent> _xenoNestSurfaceQuery;
    private EntityQuery<XenoWeedableComponent> _xenoWeedableQuery;
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _allowWeedSpreadQuery = GetEntityQuery<AllowWeedSpreadComponent>();
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
                        !_tag.HasTag(anchoredId, IgnoredTag) &&
                        !_allowWeedSpreadQuery.HasComp(anchoredId))
                    {
                        blocked = true;
                        continue;
                    }

                    if (!_map.TryGetTileRef(grid, grid, neighbor, out var tileRef) ||
                        tileRef.Tile.IsEmpty ||
                        _turf.IsSpace(tileRef))
                    {
                        blocked = true;
                        continue;
                    }

                    if (_xenoWeedsQuery.TryComp(anchoredId, out var otherWeeds))
                    {
                        if (otherWeeds.Level >= weeds.Level)
                            blocked = true;
                        else
                            weedsToReplace = anchoredId;
                    }
                }

                // Do a raycast to see if any entities with offset fixtures are blocking the spread
                var weedPosition = _transform.GetMoverCoordinates(uid).Position;
                var ray = new CollisionRay(weedPosition, cardinal.CardinalToIntVec(), (int)CollisionGroup.BarricadeImpassable);
                var intersect = _physics.IntersectRayWithPredicate(Transform(uid).MapID, ray, 0.6f, e => !Transform(e).Anchored);
                var results = intersect.Select(r => r.HitEntity).ToHashSet();

                if (results.Count > 0)
                    blocked = true;

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
                {
                    if (sourceWeeds != null && !sourceWeeds.HasHealed)
                    {
                        sourceWeeds.HasHealed = true;
                        _damageable.TryChangeDamage(source, sourceWeeds.HealOnStopSpreading, true);
                        Dirty(source.Value, sourceWeeds);
                    }

                    break;
                }

                if (!CanSpreadWeedsPopup(grid, neighbor, null, weeds.SpreadsOnSemiWeedable))
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
                            !TryComp(anchoredId, out TransformComponent? weedableTransform) ||
                            !weedableTransform.Anchored)
                        {
                            continue;
                        }

                        var ev = new AfterEntityWeedingEvent(_entities.GetNetEntity(neighborWeeds), _entities.GetNetEntity(anchoredId));
                        RaiseLocalEvent(anchoredId, ev);

                        if (source is not null)
                            RaiseLocalEvent(source.Value, ev);

                        neighborWeedsComp.LocalWeeded.Add(anchoredId);
                        _appearance.SetData(anchoredId, WeededEntityLayers.Layer, true);

                        if (weedable.Spawn is null)
                        {
                            continue;
                        }

                        weedable.Entity = SpawnAtPosition(weedable.Spawn, anchoredId.ToCoordinates());
                        var wallWeeds = EnsureComp<XenoWallWeedsComponent>(weedable.Entity.Value);
                        wallWeeds.Weeds = source;
                        Dirty(weedable.Entity.Value, wallWeeds);

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
