using Content.Shared._RMC14.Terrain.Prototypes;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Terrain;

public sealed class RMCTerrainSystem : EntitySystem
{
    private const float ExplosionLightLayerDamageChance = 0.20f;
    private const float ExplosionHeavyLayerDamageChance = 0.60f;

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly Dictionary<string, RMCTerrainDigStage> _stages = new();
    private readonly Dictionary<string, int> _stageLayers = new();
    private EntityQuery<RMCTerrainClearableComponent> _clearableQuery;
    private bool _cachedStages;

    public override void Initialize()
    {
        _clearableQuery = GetEntityQuery<RMCTerrainClearableComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _cachedStages = false;
        _stages.Clear();
        _stageLayers.Clear();
    }

    public ContentTileDefinition GetContentTileDefinition(TileRef tile)
    {
        return (ContentTileDefinition) _tiles[tile.Tile.TypeId];
    }

    public bool TryGetTileRef(EntityCoordinates coordinates, out TileRef tile)
    {
        tile = default;
        if (!_turf.TryGetTileRef(coordinates, out var tileRef) ||
            tileRef == null)
        {
            return false;
        }

        tile = tileRef.Value;
        return true;
    }

    public bool CanDigTile(TileRef tileRef, out RMCTerrainMaterial material)
    {
        var tileDef = GetContentTileDefinition(tileRef);
        return CanDigTile(tileDef, out material);
    }

    public bool TryDamageStages(TileRef tileRef, int maxTileBreak)
    {
        if (maxTileBreak <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        if (!TryGetStageLayer(tileDef.ID, out var layer) ||
            layer <= 0)
        {
            return false;
        }

        var amount = maxTileBreak switch
        {
            1 when _random.Prob(ExplosionLightLayerDamageChance) => 1,
            2 when _random.Prob(ExplosionHeavyLayerDamageChance) => 2,
            >= 3 => layer,
            _ => 0,
        };

        return amount > 0 &&
               TryLowerStage(tileRef, amount, out _, out _);
    }

    public bool TryDamageLayer(TileRef tileRef, int maxTileBreak)
    {
        return TryDamageStages(tileRef, maxTileBreak);
    }

    public bool TryDigTile(TileRef tileRef, int batchSize, out RMCTerrainDigResult result)
    {
        result = default;
        if (batchSize <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        if (!tileDef.CanDig)
            return false;

        if (!TryGetStage(tileDef.ID, out var stage))
        {
            if (tileDef.RmcDigType is RMCTerrainMaterial.None or RMCTerrainMaterial.Snow)
                return false;

            result = new RMCTerrainDigResult(tileDef.RmcDigType, batchSize, 0, tileDef.ID);
            ClearSurfaceCover(tileRef);
            return true;
        }

        if (!stage.Diggable ||
            stage.Material == RMCTerrainMaterial.None ||
            stage.Yield <= 0)
        {
            return false;
        }

        var material = stage.Material;
        if (stage.DigTo == null)
        {
            result = new RMCTerrainDigResult(material, stage.Yield, 0, tileDef.ID);
            ClearSurfaceCover(tileRef);
            return true;
        }

        var digYield = 0;
        var removed = 0;
        var currentTile = tileDef.ID;
        var current = stage;
        ContentTileDefinition? replacement = null;

        for (var i = 0; i < batchSize && current.DigTo != null; i++)
        {
            if (!current.Diggable ||
                current.Material != material ||
                current.Yield <= 0)
            {
                break;
            }

            digYield += current.Yield;
            var nextTile = current.DigTo;
            if (!TryGetReplacementTile(nextTile, out replacement) ||
                !TryGetStage(nextTile, out var next))
            {
                break;
            }

            removed++;
            currentTile = nextTile;
            current = next;
        }

        if (digYield <= 0)
            return false;

        if (replacement != null && _net.IsServer)
        {
            _tile.ReplaceTile(tileRef, replacement);
            ClearSurfaceCover(tileRef);
        }

        result = new RMCTerrainDigResult(material, digYield, removed, currentTile);
        return true;
    }

    public bool TryLowerLayer(TileRef tileRef, int amount, out int removed, out RMCTerrainMaterial material)
    {
        return TryLowerStage(tileRef, amount, out removed, out material);
    }

    public bool TryLowerStage(TileRef tileRef, int amount, out int removed, out RMCTerrainMaterial material)
    {
        removed = 0;
        material = RMCTerrainMaterial.None;

        if (amount <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        if (!TryGetStage(tileDef.ID, out var stage) ||
            stage.DigTo == null)
            return false;

        material = stage.Material;
        var current = stage;
        ContentTileDefinition? replacement = null;
        for (var i = 0; i < amount; i++)
        {
            if (current.DigTo == null ||
                !TryGetReplacementTile(current.DigTo, out replacement) ||
                !TryGetStage(current.DigTo, out current))
            {
                break;
            }

            removed++;
        }

        if (removed <= 0 || replacement == null)
            return false;

        if (_net.IsServer)
        {
            _tile.ReplaceTile(tileRef, replacement);
            ClearSurfaceCover(tileRef);
        }

        return true;
    }

    public bool TryRaiseLayer(TileRef tileRef, int amount, int maxLayer, out int added)
    {
        return TryRaiseStage(tileRef, amount, maxLayer, out added);
    }

    public bool TryRaiseStage(TileRef tileRef, int amount, int maxLayer, out int added)
    {
        added = 0;

        if (amount <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        if (!TryGetStage(tileDef.ID, out var stage) ||
            stage.PlaceTo == null)
            return false;

        var current = stage;
        ContentTileDefinition? replacement = null;
        for (var i = 0; i < amount; i++)
        {
            if (current.PlaceTo == null ||
                !TryGetStageLayer(current.PlaceTo, out var nextLayer) ||
                nextLayer > maxLayer ||
                !TryGetReplacementTile(current.PlaceTo, out replacement) ||
                !TryGetStage(current.PlaceTo, out current))
            {
                break;
            }

            added++;
        }

        if (added <= 0 || replacement == null)
            return false;

        if (_net.IsServer)
            _tile.ReplaceTile(tileRef, replacement);

        return true;
    }

    public bool TryLowerLayer(EntityCoordinates coordinates, int amount, out int removed, out RMCTerrainMaterial material)
    {
        removed = 0;
        material = RMCTerrainMaterial.None;

        if (!TryGetTileRef(coordinates, out var tile))
            return false;

        return TryLowerLayer(tile, amount, out removed, out material);
    }

    public bool TryRaiseLayer(EntityCoordinates coordinates, int amount, int maxLayer, out int added)
    {
        added = 0;

        if (!TryGetTileRef(coordinates, out var tile))
            return false;

        return TryRaiseLayer(tile, amount, maxLayer, out added);
    }

    public EntityCoordinates GetTileCoordinates(Entity<MapGridComponent> grid, TileRef tile)
    {
        return _map.GridTileToLocal(grid, grid, tile.GridIndices);
    }

    public bool IsStagedTile(TileRef tileRef)
    {
        var tileDef = GetContentTileDefinition(tileRef);
        return TryGetStage(tileDef.ID, out _);
    }

    public bool IsStageAboveBase(TileRef tileRef)
    {
        var tileDef = GetContentTileDefinition(tileRef);
        return TryGetStageLayer(tileDef.ID, out var layer) &&
               layer > 0;
    }

    private bool CanDigTile(ContentTileDefinition tileDef, out RMCTerrainMaterial material)
    {
        material = RMCTerrainMaterial.None;
        if (!tileDef.CanDig)
            return false;

        if (TryGetStage(tileDef.ID, out var stage))
        {
            material = stage.Material;
            return stage.Diggable &&
                   material != RMCTerrainMaterial.None &&
                   stage.Yield > 0;
        }

        material = tileDef.RmcDigType;
        return material != RMCTerrainMaterial.None &&
               material != RMCTerrainMaterial.Snow;
    }

    private bool TryGetStage(string tile, out RMCTerrainDigStage stage)
    {
        EnsureStageCache();
        return _stages.TryGetValue(tile, out stage!);
    }

    private bool TryGetStageLayer(string tile, out int layer)
    {
        EnsureStageCache();
        return _stageLayers.TryGetValue(tile, out layer);
    }

    private bool TryGetReplacementTile(string tile, out ContentTileDefinition replacement)
    {
        if (_tiles.TryGetDefinition(tile, out var definition) &&
            definition is ContentTileDefinition content)
        {
            replacement = content;
            return true;
        }

        replacement = default!;
        return false;
    }

    private void ClearSurfaceCover(TileRef tileRef)
    {
        if (!_net.IsServer ||
            !TryComp(tileRef.GridUid, out MapGridComponent? grid))
        {
            return;
        }

        var anchored = _map.GetAnchoredEntitiesEnumerator(tileRef.GridUid, grid, tileRef.GridIndices);
        while (anchored.MoveNext(out var uid))
        {
            if (_clearableQuery.HasComp(uid))
                QueueDel(uid);
        }
    }

    private void EnsureStageCache()
    {
        if (_cachedStages)
            return;

        _stages.Clear();
        _stageLayers.Clear();

        foreach (var graph in _prototypes.EnumeratePrototypes<RMCTerrainDigGraphPrototype>())
        {
            foreach (var stage in graph.Stages)
            {
                if (!_stages.TryAdd(stage.Tile, stage))
                    continue;
            }
        }

        foreach (var tile in _stages.Keys)
        {
            _stageLayers[tile] = CalculateStageLayer(tile);
        }

        _cachedStages = true;
    }

    private int CalculateStageLayer(string tile)
    {
        var layer = 0;
        var current = tile;
        var visited = new HashSet<string>();

        while (_stages.TryGetValue(current, out var stage) &&
               stage.DigTo != null &&
               visited.Add(current))
        {
            layer++;
            current = stage.DigTo;
        }

        return layer;
    }
}

public readonly record struct RMCTerrainDigResult(
    RMCTerrainMaterial Material,
    int Yield,
    int RemovedStages,
    string ResultTile);
