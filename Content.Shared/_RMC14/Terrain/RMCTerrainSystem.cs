using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Terrain;

public sealed class RMCTerrainSystem : EntitySystem
{
    public const string SnowLayerSet = "Snow";
    public const string BrownSnowLayerSet = "BrownSnow";
    public const int SnowHandPlaceMaxLayer = 3;
    private const float XenoSnowSlowModifier = 3f / 7f;
    private const float ExplosionLightLayerDamageChance = 0.20f;
    private const float ExplosionHeavyLayerDamageChance = 0.60f;

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly Dictionary<(string Set, int Layer), ContentTileDefinition> _layerTiles = new();
    private bool _cachedLayers;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSpeciesSlowdownModifierComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<XenoComponent, AfterInteractEvent>(OnXenoAfterInteract);
        SubscribeLocalEvent<XenoComponent, XenoClearSnowDoAfterEvent>(OnXenoClearSnowDoAfter);
    }

    private void OnXenoAfterInteract(Entity<XenoComponent> xeno, ref AfterInteractEvent args)
    {
        if (_net.IsClient ||
            args.Handled ||
            !args.CanReach ||
            args.Target != null ||
            xeno.Comp.Tier <= 0)
        {
            return;
        }

        if (!CanXenoClearSnow(xeno, args.ClickLocation, out _))
            return;

        args.Handled = true;
        StartXenoClearSnowDoAfter(xeno, args.ClickLocation);
    }

    private void OnXenoClearSnowDoAfter(Entity<XenoComponent> xeno, ref XenoClearSnowDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!CanXenoClearSnow(xeno, coordinates, out var tile) ||
            !_interaction.InRangeUnobstructed(xeno, coordinates, popup: false) ||
            !TryLowerLayer(tile, 1, out _, out _))
        {
            return;
        }

        if (CanXenoClearSnow(xeno, coordinates, out _))
            StartXenoClearSnowDoAfter(xeno, coordinates);
    }

    private void OnMove(Entity<RMCSpeciesSlowdownModifierComponent> ent, ref MoveEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetTileRef(args.OldPosition, out var oldTile) ||
            !TryGetTileRef(args.NewPosition, out var newTile))
        {
            return;
        }

        if (oldTile.GridUid == newTile.GridUid &&
            oldTile.GridIndices == newTile.GridIndices)
        {
            return;
        }

        var tileDef = GetContentTileDefinition(newTile);
        if (!IsSnow(tileDef) ||
            tileDef.RmcTerrainLayer <= 0)
        {
            return;
        }

        var isXeno = HasComp<XenoComponent>(ent);
        var modifier = isXeno ? XenoSnowSlowModifier : 1f;
        var slow = tileDef.RmcSnowSlowSeconds * modifier;
        if (slow > 0)
            _slow.TrySlowdown(ent, TimeSpan.FromSeconds(slow));

        var superSlow = tileDef.RmcSnowSuperSlowSeconds * modifier;
        if (!isXeno &&
            superSlow > 0 &&
            tileDef.RmcSnowSuperSlowChance > 0 &&
            _random.Prob(tileDef.RmcSnowSuperSlowChance))
        {
            _slow.TrySuperSlowdown(ent, TimeSpan.FromSeconds(superSlow));
        }
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

    public bool IsSnow(ContentTileDefinition tile)
    {
        return tile.RmcTerrainLayerSet is SnowLayerSet or BrownSnowLayerSet;
    }

    public bool TryLowerSnowLayer(EntityCoordinates coordinates, int amount, out int removed)
    {
        removed = 0;

        if (!TryGetTileRef(coordinates, out var tile))
            return false;

        var tileDef = GetContentTileDefinition(tile);
        if (!IsSnow(tileDef))
            return false;

        return TryLowerLayer(tile, amount, out removed, out _);
    }

    public bool TryDamageLayer(TileRef tileRef, int maxTileBreak)
    {
        if (maxTileBreak <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        if (tileDef.RmcTerrainLayerSet == null ||
            tileDef.RmcTerrainLayer <= 0)
        {
            return false;
        }

        var amount = maxTileBreak switch
        {
            1 when _random.Prob(ExplosionLightLayerDamageChance) => 1,
            2 when _random.Prob(ExplosionHeavyLayerDamageChance) => 2,
            >= 3 => tileDef.RmcTerrainLayer,
            _ => 0,
        };

        return amount > 0 &&
               TryLowerLayer(tileRef, amount, out _, out _);
    }

    public bool TryLowerLayer(TileRef tileRef, int amount, out int removed, out RMCTerrainMaterial material)
    {
        removed = 0;
        material = RMCTerrainMaterial.None;

        if (amount <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        material = tileDef.RmcDigType;
        if (tileDef.RmcTerrainLayerSet == null ||
            tileDef.RmcTerrainLayer <= 0)
        {
            return false;
        }

        var targetLayer = Math.Max(0, tileDef.RmcTerrainLayer - amount);
        if (!TryGetLayerTile(tileDef.RmcTerrainLayerSet, targetLayer, out var replacement))
            return false;

        removed = tileDef.RmcTerrainLayer - targetLayer;
        if (removed <= 0)
            return false;

        if (_net.IsServer)
            _tile.ReplaceTile(tileRef, replacement);

        return true;
    }

    public bool TryRaiseLayer(TileRef tileRef, int amount, int maxLayer, out int added)
    {
        added = 0;

        if (amount <= 0)
            return false;

        var tileDef = GetContentTileDefinition(tileRef);
        if (tileDef.RmcTerrainLayerSet == null)
            return false;

        var targetLayer = Math.Min(maxLayer, tileDef.RmcTerrainLayer + amount);
        if (targetLayer <= tileDef.RmcTerrainLayer ||
            !TryGetLayerTile(tileDef.RmcTerrainLayerSet, targetLayer, out var replacement))
        {
            return false;
        }

        added = targetLayer - tileDef.RmcTerrainLayer;
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

    private bool CanXenoClearSnow(Entity<XenoComponent> xeno, EntityCoordinates coordinates, out TileRef tile)
    {
        tile = default;
        if (xeno.Comp.Tier <= 0 ||
            !TryGetTileRef(coordinates, out tile))
        {
            return false;
        }

        var tileDef = GetContentTileDefinition(tile);
        return IsSnow(tileDef) && tileDef.RmcTerrainLayer > 0;
    }

    private void StartXenoClearSnowDoAfter(Entity<XenoComponent> xeno, EntityCoordinates coordinates)
    {
        var seconds = Math.Max(0.4f, 1.2f / Math.Max(xeno.Comp.Tier, 1));
        var ev = new XenoClearSnowDoAfterEvent(GetNetCoordinates(coordinates));
        var doAfter = new DoAfterArgs(EntityManager, xeno, TimeSpan.FromSeconds(seconds), ev, xeno)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private bool TryGetLayerTile(string set, int layer, out ContentTileDefinition tile)
    {
        EnsureLayerCache();
        return _layerTiles.TryGetValue((set, layer), out tile!);
    }

    private void EnsureLayerCache()
    {
        if (_cachedLayers)
            return;

        _layerTiles.Clear();
        foreach (var definition in _tiles)
        {
            if (definition is not ContentTileDefinition content ||
                content.RmcTerrainLayerSet == null)
            {
                continue;
            }

            _layerTiles.TryAdd((content.RmcTerrainLayerSet, content.RmcTerrainLayer), content);
        }

        _cachedLayers = true;
    }
}
