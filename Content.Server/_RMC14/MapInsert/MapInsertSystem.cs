﻿using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Map;
using Content.Shared.GameTicking;
using Robust.Server.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.MapInsert;

public sealed class MapInsertSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GridFixtureSystem _fixture = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private MapId? _map;
    private int _index;

    private readonly HashSet<EntityUid> _lookupEnts = new();
    private readonly HashSet<EntityUid> _immuneEnts = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<MapInsertComponent, MapInitEvent>(OnMapInsertMapInit, before: [typeof(AreaSystem)]);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _map = null;
        _index = 0;
    }

    private void OnMapInsertMapInit(Entity<MapInsertComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_random.Prob(ent.Comp.Probability))
        {
            QueueDel(ent);
            return;
        }

        if (ent.Comp.Spawn is not { } spawn)
            return;

        if (_map == null)
        {
            _mapSystem.CreateMap(out var mapId);
            _map = mapId;
        }

        var matrix = Matrix3x2.CreateTranslation(_index * 50, _index * 50);
        _index++;

        _rmcMap.TryLoad(_map.Value, spawn.ToString(), out var grids, matrix);
        if (grids == null || grids.Count == 0)
            return;

        var insertGrid = grids[0];
        var xform = Transform(ent);
        var mainGrid = xform.GridUid;
        var coordinates = _transform.GetMapCoordinates(ent, xform).Offset(new Vector2(-0.5f, -0.5f));
        coordinates = coordinates.Offset(ent.Comp.Offset);
        var coordinatesi = new Vector2i((int)coordinates.X, (int)coordinates.Y);

        //Replace areas
        if (ent.Comp.ReplaceAreas)
        {
            if (EntityManager.TryGetComponent(mainGrid, out AreaGridComponent? mainAreaGrid)
                && EntityManager.TryGetComponent(insertGrid, out AreaGridComponent? insertAreaGrid))
            {
                foreach (var (position, protoId) in insertAreaGrid.Areas)
                {
                    if (!_prototypes.TryIndex(protoId, out var proto))
                        continue;

                    if (!proto.TryGetComponent(out AreaComponent? areaComp, _compFactory))
                        continue;

                    _areas.ReplaceArea(mainAreaGrid, coordinatesi + position, protoId);
                }
            }
        }


        // Clear all entities on map in insert area
        _transform.SetMapCoordinates(insertGrid, coordinates);
        if (ent.Comp.ClearEntities)
        {
            MapInsertSmimsh(insertGrid);
        }

        // Merge grids
        if (mainGrid == null)
            return;
        // Need to make sure the grid isn't overlapping where it's going to be merged to, otherwise exception
        _transform.SetMapCoordinates(insertGrid, coordinates.Offset(new Vector2(999f)));
        _fixture.Merge((EntityUid)mainGrid, insertGrid, coordinatesi, Angle.Zero);

        QueueDel(ent);
    }

    private void MapInsertSmimsh(EntityUid uid,  FixturesComponent? manager = null, MapGridComponent? grid = null, TransformComponent? xform = null)
    {
        // This code is based on the Smimsh function for shuttle ftl, but we need some tweaks for our use-case

        if (!Resolve(uid, ref manager, ref grid, ref xform) || xform.MapUid == null)
            return;

        // Flatten anything not parented to a grid.
        var aabbs = new List<Box2>(manager.Fixtures.Count);
        var tileSet = new List<(Vector2i, Tile)>();

        var tiles = new HashSet<Vector2i>();
        if (TryComp(uid, out MapGridComponent? shuttleGrid))
        {
            var enumerator = _mapSystem.GetAllTilesEnumerator(uid, shuttleGrid);
            while (enumerator.MoveNext(out var tile))
            {
                tiles.Add(tile.Value.GridIndices);
            }
        }

        foreach (var fixture in manager.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            var aabb = _physics.GetWorldAABB(uid, xform: xform);

            aabb = aabb.Enlarged(-0.05f);
            aabbs.Add(aabb);

            tileSet.Clear();
            _lookupEnts.Clear();
            _immuneEnts.Clear();
            // TODO: Ideally we'd query first BEFORE moving grid but needs adjustments above.
            _lookup.GetLocalEntitiesIntersecting(xform.MapUid.Value, aabb, _lookupEnts, flags: LookupFlags.Uncontained);

            foreach (var ent in _lookupEnts)
            {
                if (ent == uid || _immuneEnts.Contains(ent))
                {
                    continue;
                }

                // If it's on our grid ignore it.
                if (!TryComp(ent, out TransformComponent? childXform) || childXform.GridUid == uid)
                {
                    continue;
                }

                if (HasComp<AreaComponent>(ent))
                    continue;

                QueueDel(ent);
            }
        }
    }
}
