using System.Numerics;
using Content.Server.Atmos.Components;
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
using Content.Shared.Coordinates.Helpers;
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
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;

    private TimeSpan _maxProcessTime;

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _allowWeedSpreadQuery = GetEntityQuery<AllowWeedSpreadComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _xenoNestSurfaceQuery = GetEntityQuery<XenoNestSurfaceComponent>();
        _xenoWeedsQuery = GetEntityQuery<XenoWeedsComponent>();

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
                        if (!WeedableQuery.TryComp(anchoredId, out var weedable) ||
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

                        if (weedable.Spawn == null)
                            continue;

                        weedable.Entity = SpawnAtPosition(weedable.Spawn, anchoredId.ToCoordinates());
                        var wallWeeds = EnsureComp<XenoWallWeedsComponent>(weedable.Entity.Value);
                        wallWeeds.Weeds = neighborWeeds;
                        wallWeeds.AttachedTo = anchoredId;
                        Dirty(weedable.Entity.Value, wallWeeds);

                        if (_xenoNestSurfaceQuery.TryComp(weedable.Entity, out var surface))
                        {
                            surface.Weedable = anchoredId;
                            Dirty(weedable.Entity.Value, surface);
                        }
                    }
                }
            }
        }

        var decayingQuery = EntityQueryEnumerator<XenoWeedsDecayingComponent>();
        while (decayingQuery.MoveNext(out var uid, out var comp))
        {
            comp.Lifetime -= frameTime;

            // Before despawning the weed entity, check to see if there's a different node in range which could take over as its parent.
            if (comp.Lifetime <= 0 && !TryAvoidOrphanage(uid))
            {
                QueueDel(uid);
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

    private bool TryAvoidOrphanage(EntityUid weedEnt)
    {
        if (!WeedsQuery.TryComp(weedEnt, out var weedsComp))
            return false;

        if (FindNewParentNode((weedEnt, weedsComp)) is not { } newParent)
            return false;
        // Found a new parent! (yippee)

        RemComp<XenoWeedsDecayingComponent>(weedEnt);

        weedsComp.Source = newParent;
        Dirty(weedEnt, weedsComp);

        newParent.Comp.Spread.Add(weedEnt);
        Dirty(newParent, newParent.Comp);
        return true;
    }

    private Entity<XenoWeedsComponent>? FindNewParentNode(Entity<XenoWeedsComponent> childEnt)
    {
        Entity<XenoWeedsComponent>? newParent = null;

        var coordinates = _transform.GetMoverCoordinates(childEnt).SnapToGrid(EntityManager, Map);
        if (_transform.GetGrid(coordinates) is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComp))
            return newParent;

        HiveMemberQuery.TryComp(childEnt, out var weedHive);
        // "Re-parenting" (adoption?) range is deliberately one smaller than the standard growth range.
        foreach (var node in GetNearbyWeeds((gridUid, gridComp), coordinates, childEnt.Comp.Range - 1))
        {
            // Nodes can't support weeds which are too strong for them. (Hive weeds or "Hardy" weeds)
            if (node.Comp.Level < childEnt.Comp.Level)
                continue;

            // If both weed entities are part of a hive, make sure they're in the same one. (Hiveless weeds can be taken without issue)
            if (weedHive != null && HiveMemberQuery.TryComp(node, out var nodeHive) && !_hive.IsMember((node, nodeHive), weedHive.Hive))
                continue;

            newParent = node;
            break;
        }
        return newParent;
    }
}
