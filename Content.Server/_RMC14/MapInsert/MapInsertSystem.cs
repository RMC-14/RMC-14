using System.Numerics;
using Content.Server._RMC14.Rules;
using Content.Server.Decals;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Rules;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Robust.Server.Physics;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.MapInsert;

public sealed class MapInsertSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly GridFixtureSystem _fixture = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;

    private MapId? _map;
    private int _index;

    private readonly HashSet<EntityUid> _lookupEnts = new();
    private readonly HashSet<EntityUid> _immuneEnts = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _map = null;
        _index = 0;
    }

    public string SelectMapScenario(List<RMCNightmareScenario> scenarioList)
    {
        if (scenarioList.Count <= 0)
        {
            return string.Empty;
        }

        var randomProbability = _random.NextFloat();
        var cumulativeProbability = 0f;

        foreach (var scenario in scenarioList)
        {
            cumulativeProbability += scenario.ScenarioProbability;
            if (cumulativeProbability >= randomProbability)
            {
                return scenario.ScenarioName;
            }
        }

        return string.Empty;
    }

    public void ProcessMapInsert(Entity<MapInsertComponent> ent, bool forceSpawn = false)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Variations.Count <= 0)
        {
            QueueDel(ent);
            return;
        }

        var randomProbability = _random.NextFloat();
        var cumulativeProbability = 0f;
        ResPath spawn = default;
        Vector2 spawnOffset = default;
        foreach (var variation in ent.Comp.Variations)
        {
            cumulativeProbability += variation.Probability;
            if (!forceSpawn &&
                ((variation.NightmareScenario != _distressSignal.ActiveNightmareScenario) ||
                 cumulativeProbability < randomProbability))
                continue;
            spawn = variation.Spawn;
            spawnOffset = variation.Offset;
            break;
        }

        if (spawn == default)
        {
            QueueDel(ent);
            return;
        }

        if (_map == null)
        {
            _mapSystem.CreateMap(out var mapId);
            _map = mapId;
        }

        var offset = new Vector2(_index * 50, _index * 50);
        _index++;

        if (!_mapLoader.TryLoadGrid(_map.Value, spawn, out var grid, offset: offset))
            return;

        var insertGrid = grid.Value;
        var xform = Transform(ent);
        var mainGrid = xform.GridUid;
        if (mainGrid == null)
            return;
        var coordinates = _transform.GetMapCoordinates(ent, xform).Offset(new Vector2(-0.5f, -0.5f));
        coordinates = coordinates.Offset(spawnOffset);
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
        MapInsertSmimsh(insertGrid, (EntityUid)mainGrid, ent.Comp.ClearEntities, ent.Comp.ClearDecals);

        // Merge grids
        // Need to make sure the grid isn't overlapping where it's going to be merged to, otherwise exception
        _transform.SetMapCoordinates(insertGrid, coordinates.Offset(new Vector2(999f)));

        //Decals not handled in Merge(), so do it here
        if (!TryComp(insertGrid, out DecalGridComponent? insertDecalGrid))
            return;

        foreach (var chunk in insertDecalGrid.ChunkCollection.ChunkCollection.Values)
        {
            foreach (var (decalUid, decal) in chunk.Decals)
            {
                _decals.SetDecalPosition(insertGrid, decalUid, new(mainGrid.Value, decal.Coordinates + coordinatesi));
            }
        }

        _fixture.Merge((EntityUid)mainGrid, insertGrid, coordinatesi, Angle.Zero);

        QueueDel(ent);
    }

    private void MapInsertSmimsh(EntityUid uid, EntityUid mainGridUid, bool clearEntities, bool clearDecals, FixturesComponent? manager = null, MapGridComponent? grid = null, TransformComponent? xform = null)
    {
        // This code is based on the Smimsh function for shuttle ftl, but we need some tweaks for our use-case
        if (!(clearEntities || clearDecals))
            return;

        if (!Resolve(uid, ref manager, ref grid, ref xform) || xform.MapUid == null)
            return;

        // Flatten anything not parented to a grid.
        var transform = _physics.GetRelativePhysicsTransform((uid, xform), xform.MapUid.Value);

        foreach (var fixture in manager.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            var aabb = fixture.Shape.ComputeAABB(transform, 0);

            aabb = aabb.Enlarged(-0.05f);
            _lookupEnts.Clear();
            _immuneEnts.Clear();
            // TODO: Ideally we'd query first BEFORE moving grid but needs adjustments above.
            _lookup.GetLocalEntitiesIntersecting(xform.MapUid.Value, aabb, _lookupEnts, flags: LookupFlags.Uncontained);

            if (clearEntities)
            {
                foreach (var ent in _lookupEnts)
                {
                    if (ent == uid || _immuneEnts.Contains(ent))
                        continue;

                    // If it's on our grid ignore it.
                    if (!TryComp(ent, out TransformComponent? childXform) || childXform.GridUid == uid)
                        continue;

                    if (HasComp<AreaComponent>(ent))
                        continue;

                    QueueDel(ent);
                }
            }

            if (clearDecals)
            {
                var mainGridDecals = _decals.GetDecalsIntersecting(mainGridUid, aabb);
                foreach (var decal in mainGridDecals)
                {
                    _decals.RemoveDecal(mainGridUid, decal.Index);
                }
            }
        }
    }
}
