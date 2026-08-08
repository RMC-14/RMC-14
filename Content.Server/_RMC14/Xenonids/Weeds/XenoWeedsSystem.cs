using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Construction;
using Content.Server.Spreader;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Communications;
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
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Weeds;

public sealed class XenoWeedsSystem : SharedXenoWeedsSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDirectionalAttackBlockSystem _directionBlocker = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly ProtoId<TagPrototype> IgnoredTag = "SpreaderIgnore";

    private readonly List<EntityUid> _anchored = new();
    private readonly List<Entity<XenoWeedsComponent>> _spread = new();

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<AllowWeedSpreadComponent> _allowWeedSpreadQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<XenoNestSurfaceComponent> _xenoNestSurfaceQuery;
    private EntityQuery<XenoWeedableComponent> _xenoWeedableQuery;
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;

    private TimeSpan _maxProcessTime;

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _allowWeedSpreadQuery = GetEntityQuery<AllowWeedSpreadComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _xenoNestSurfaceQuery = GetEntityQuery<XenoNestSurfaceComponent>();
        _xenoWeedableQuery = GetEntityQuery<XenoWeedableComponent>();
        _xenoWeedsQuery = GetEntityQuery<XenoWeedsComponent>();

        SubscribeLocalEvent<XenoWeedableComponent, ConstructionChangeEntityEvent>(OnConstructionChangeEntity);

        Subs.CVar(
            _config,
            RMCCVars.RMCWeedSpreadMaxProcessTimeMilliseconds,
            v => _maxProcessTime = TimeSpan.FromMilliseconds(v),
            true
        );
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        for (var i = _spread.Count - 1; i >= 0; i--)
        {
            if (_timing.CurTime - time > _maxProcessTime)
                return;

            var (uid, weeds) = _spread[i];
            _spread.RemoveAt(i);

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

                if (_directionBlocker.IsDirectionBlocked(uid,
                        cardinal,
                        collisionGroup: CollisionGroup.BarricadeImpassable))
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

                if (!CanSpreadWeedsPopup(grid, neighbor, null, null, weeds.SpreadsOnSemiWeedable))
                    continue;

                if (weedsToReplace != null)
                    QueueDel(weedsToReplace.Value);

                var coords = _map.GridTileToLocal(grid, grid, neighbor);
                var neighborWeeds = Spawn(prototype, coords);
                var neighborWeedsEnt = AssignSource(neighborWeeds, (source.Value, sourceWeeds));

                _hive.SetSameHive(uid, neighborWeeds);

                EnsureComp<ActiveEdgeSpreaderComponent>(neighborWeeds);

                for (var j = 0; j < 4; j++)
                {
                    var dir = (AtmosDirection)(1 << j);
                    var pos = neighbor.Offset(dir);
                    if (!_map.TryGetTileRef(grid, grid, pos, out var adjacent))
                        continue;

                    _anchored.Clear();
                    _map.GetAnchoredEntities(grid, adjacent.GridIndices, _anchored);
                    foreach (var anchoredId in _anchored)
                    {
                        if (!_xenoWeedableQuery.TryComp(anchoredId, out var weedable) ||
                            !TryComp(anchoredId, out TransformComponent? weedableTransform) ||
                            !weedableTransform.Anchored ||
                            weedable.Entity != null)
                        {
                            continue;
                        }

                        if (source != null)
                        {
                            var ev = new AfterEntityWeedingEvent(neighborWeeds, anchoredId);
                            RaiseLocalEvent(source.Value, ref ev);
                        }

                        neighborWeedsEnt.Comp.LocalWeeded.Add(anchoredId);

                        if (!HasComp<CommunicationsTowerComponent>(anchoredId))
                            _appearance.SetData(anchoredId, WeededEntityLayers.Layer, true);

                        var wallWeeds = SpawnWallWeeds((anchoredId, weedable), source);
                        if (wallWeeds is { } spawnedWeeds)
                            sourceWeeds?.Spread.Add(spawnedWeeds.Owner);
                    }
                }
            }
        }

        if (_spread.Count > 0)
            return;

        var spreadingQuery = EntityQueryEnumerator<XenoWeedsSpreadingComponent, XenoWeedsComponent>();
        while (spreadingQuery.MoveNext(out var uid, out var spreading, out var weeds))
        {
            if (time < spreading.SpreadAt)
                continue;

            RemCompDeferred<XenoWeedsSpreadingComponent>(uid);
            _spread.Add((uid, weeds));
        }
    }

    private Entity<XenoWallWeedsComponent>? SpawnWallWeeds(Entity<XenoWeedableComponent> wallToWeed, EntityUid? sourceWeeds)
    {
        if (wallToWeed.Comp.Spawn == null)
            return null;

        var spawnedWeeds = SpawnAtPosition(wallToWeed.Comp.Spawn, wallToWeed.Owner.ToCoordinates());
        wallToWeed.Comp.Entity = spawnedWeeds;

        var wallWeeds = EnsureComp<XenoWallWeedsComponent>(spawnedWeeds);
        wallWeeds.Weeds = sourceWeeds;
        wallWeeds.WeededSurface = wallToWeed;
        Dirty(spawnedWeeds, wallWeeds);

        if (_xenoNestSurfaceQuery.TryComp(spawnedWeeds, out var nestSurface))
        {
            nestSurface.Weedable = wallToWeed;
            Dirty(spawnedWeeds, nestSurface);
        }

        return (spawnedWeeds, wallWeeds);
    }

    // Transfer wall weeds over when one `XenoWeedable` entity gets turned into another. (E.g. Window into window frame)
    private void OnConstructionChangeEntity(Entity<XenoWeedableComponent> ent, ref ConstructionChangeEntityEvent args)
    {
        if (ent.Owner != args.New)
            return;

        if (!_xenoWeedableQuery.TryComp(args.Old, out var oldWeedable) ||
            !TryComp<XenoWallWeedsComponent>(oldWeedable.Entity, out var oldWallWeeds))
            return;

        if (ent.Comp is not { Entity: null } newWeedable)
            return;

        // If the new and old entities both spawn the same thing when weeded, just transfer the old one over.
        if (oldWeedable.Spawn == newWeedable.Spawn)
        {
            newWeedable.Entity = oldWeedable.Entity;
            oldWeedable.Entity = null;
            Dirty(args.New, newWeedable);
            Dirty(args.Old, oldWeedable);

            if (_xenoNestSurfaceQuery.TryComp(newWeedable.Entity, out var nestSurface))
            {
                nestSurface.Weedable = args.New;
                Dirty(newWeedable.Entity.Value, nestSurface);
            }
            return;
        }

        // Otherwise, make a new one.
        var spawned = SpawnWallWeeds((args.New, newWeedable), oldWallWeeds.Weeds);
        if (spawned is { } spawnedWeeds && _xenoWeedsQuery.TryComp(oldWallWeeds.Weeds, out var sourceWeeds))
        {
            sourceWeeds.Spread.Add(spawnedWeeds.Owner);
            Dirty(oldWallWeeds.Weeds.Value, sourceWeeds);
        }
        // Removal of the old weeds is handled separately when `args.Old` is swapped out and deleted.
    }
}
