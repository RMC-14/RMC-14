using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Map;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.MapInsert;

public class SharedMapInsertSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private MapId? _map;
    private int _index;

    private readonly HashSet<EntityUid> _lookupEnts = new();
    private readonly HashSet<EntityUid> _immuneEnts = new();

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<MapInsertComponent, MapInitEvent>(OnMapInsertMapInit);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _map = null;
        _index = 0;
    }

    private void OnMapInsertMapInit(Entity<MapInsertComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Spawn is not { } spawn)
            return;

        if (_net.IsClient)
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

        var grid = grids[0];
        var xform = Transform(ent);
        var coordinates = _transform.GetMapCoordinates(ent, xform).Offset(new Vector2(-0.5f, -0.5f));
        coordinates = coordinates.Offset(ent.Comp.Offset);
        _transform.SetMapCoordinates(grid, coordinates);

        if (ent.Comp.ClearEntities)
        {
            MapInsertSmimsh(grid);
        }

        if (TryComp(grid, out PhysicsComponent? physics) &&
            TryComp(grid, out FixturesComponent? fixtures))
        {
            _physics.SetBodyType(grid, BodyType.Static, manager: fixtures, body: physics);
            _physics.SetBodyStatus(grid, physics, BodyStatus.OnGround);
            _physics.SetFixedRotation(grid, true, manager: fixtures, body: physics);
        }
    }

    private void MapInsertSmimsh(EntityUid uid,  FixturesComponent? manager = null, MapGridComponent? grid = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref manager, ref grid, ref xform) || xform.MapUid == null)
            return;

        if (!TryComp(xform.MapUid, out BroadphaseComponent? lookup))
            return;

        // Flatten anything not parented to a grid.
        var transform = _physics.GetRelativePhysicsTransform((uid, xform), xform.MapUid.Value);
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

            aabbs.Add(aabb);

            tileSet.Clear();
            _lookupEnts.Clear();
            _immuneEnts.Clear();
            // TODO: Ideally we'd query first BEFORE moving grid but needs adjustments above.
            _lookup.GetLocalEntitiesIntersecting(xform.MapUid.Value, fixture.Shape, transform, _lookupEnts, flags: LookupFlags.Uncontained, lookup: lookup);

            foreach (var ent in _lookupEnts)
            {
                if (ent == uid || _immuneEnts.Contains(ent))
                {
                    continue;
                }

                // If it's on our grid ignore it.
                if (!TryComp(ent, out TransformComponent? childXform) || childXform.GridUid == uid)
                {
                    // _transform.SetParent(ent, );
                    continue;
                }

                if (HasComp<AreaComponent>(ent))
                    continue;

                QueueDel(ent);
            }
        }
    }
}
