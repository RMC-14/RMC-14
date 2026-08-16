#if !FULL_RELEASE || RMC_MCP
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared._RMC14.Areas;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._RMC14.Mcp.Tools;

public sealed class ListMapsTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_maps";

    public override string Description =>
        "Lists all maps: numeric map id, name, whether the map is initialized (post-mapinit) and paused, " +
        "and its grids. Maps being edited for saving must stay uninitialized and paused.";

    public override JsonObject Annotations => Annotate.ReadOnly();

    public override JsonObject InputSchema => Schema.Object(new JsonObject());

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var maps = new JsonArray();
            foreach (var mapId in mapSystem.GetAllMapIds().OrderBy(m => m.GetHashCode()))
            {
                var grids = new JsonArray();
                foreach (var grid in Ctx.MapManager.GetAllGrids(mapId))
                {
                    grids.Add(Ctx.ToNetId(grid.Owner));
                }

                var entry = new JsonObject
                {
                    ["map_id"] = int.Parse(mapId.ToString()),
                    ["initialized"] = mapSystem.IsInitialized(mapId),
                    ["paused"] = mapSystem.IsPaused(mapId),
                    ["grids"] = grids,
                };

                if (mapSystem.TryGetMap(mapId, out var mapUid))
                {
                    entry["map_entity"] = Ctx.ToNetId(mapUid.Value);
                    entry["name"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(mapUid.Value).EntityName;
                }

                maps.Add(entry);
            }

            return new JsonObject { ["maps"] = maps };
        });
    }
}

public sealed class ListGridsTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "list_grids";

    public override string Description =>
        "Lists grids (station structures) with their NetEntity id, name, map, world position, tile bounds and " +
        "tile count. Grid ids are what read_tiles / set_tiles and console commands expect.";

    public override JsonObject Annotations => Annotate.ReadOnly();

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["map_id"] = Schema.Int("Only list grids on this map id (default: all maps)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var mapFilter = McpContext.OptMapId(args);

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
            var grids = new JsonArray();

            var mapIds = mapFilter is { } m
                ? new[] { new MapId(m) }
                : mapSystem.GetAllMapIds().ToArray();

            foreach (var mapId in mapIds)
            {
                foreach (var grid in Ctx.MapManager.GetAllGrids(mapId))
                {
                    var uid = grid.Owner;
                    var tileCount = 0;
                    // Compute bounds from the tiles themselves: LocalAABB is maintained by
                    // physics and can stay zero on a paused/uninitialized map.
                    var minX = int.MaxValue;
                    var minY = int.MaxValue;
                    var maxX = int.MinValue;
                    var maxY = int.MinValue;
                    var enumerator = mapSystem.GetAllTilesEnumerator(uid, grid.Comp);
                    while (enumerator.MoveNext(out var tile))
                    {
                        tileCount++;
                        var indices = tile!.Value.GridIndices;
                        minX = Math.Min(minX, indices.X);
                        minY = Math.Min(minY, indices.Y);
                        maxX = Math.Max(maxX, indices.X);
                        maxY = Math.Max(maxY, indices.Y);
                    }

                    var worldPos = transformSystem.GetWorldPosition(uid);
                    grids.Add(new JsonObject
                    {
                        ["grid"] = Ctx.ToNetId(uid),
                        ["name"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid).EntityName,
                        ["map_id"] = int.Parse(mapId.ToString()),
                        ["world_x"] = MathF.Round(worldPos.X, 1),
                        ["world_y"] = MathF.Round(worldPos.Y, 1),
                        ["world_rotation_deg"] = Math.Round(transformSystem.GetWorldRotation(uid).Degrees, 1),
                        ["tile_bounds"] = tileCount == 0
                            ? "empty"
                            : $"({minX},{minY})..({maxX},{maxY})",
                        ["tile_count"] = tileCount,
                    });
                }
            }

            return new JsonObject { ["grids"] = grids };
        });
    }
}

public sealed class ReadTilesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "read_tiles";

    public override string Description =>
        "Reads a rectangle of tiles from a grid as a character matrix with a legend (tile prototype ids). " +
        "World-aligned: top row = north, left column = west. In absolute form x,y is the SOUTH-WEST corner; " +
        "with 'relative' the player-offset tile is the CENTER of the rectangle.";

    public override JsonObject Annotations => Annotate.ReadOnly();

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["grid"] = Schema.Grid(),
        ["x"] = Schema.Int("South-west corner tile X (absolute form)."),
        ["y"] = Schema.Int("South-west corner tile Y (absolute form)."),
        ["relative"] = Schema.Relative("the center of the rectangle"),
        ["width"] = Schema.Int("Rectangle width in tiles (default 21)."),
        ["height"] = Schema.Int("Rectangle height in tiles (default 21)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var width = McpContext.OptInt(args, "width") ?? 21;
        var height = McpContext.OptInt(args, "height") ?? 21;
        if (width < 1 || height < 1 || (long) width * height > MaxArea)
            throw new McpToolException($"width/height must be positive with area <= {MaxArea}; split larger reads.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var (gridUid, grid, anchor) = Ctx.ResolveTilePosition(args);
            var corner = args.ContainsKey("relative")
                ? anchor - new Vector2i(width / 2, height / 2)
                : anchor;

            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var legend = new McpLegend();
            var rows = new JsonArray();

            for (var y = corner.Y + height - 1; y >= corner.Y; y--)
            {
                var row = new StringBuilder(width);
                for (var x = corner.X; x < corner.X + width; x++)
                {
                    var tileRef = mapSystem.GetTileRef(gridUid, grid, new Vector2i(x, y));
                    row.Append(tileRef.Tile.IsEmpty ? '.' : legend.Get(TileTools.TileName(Ctx, tileRef.Tile.TypeId)));
                }

                rows.Add(row.ToString());
            }

            return new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y },
                ["width"] = width,
                ["height"] = height,
                ["orientation"] = "world-aligned: top row = north, left column = west",
                ["legend"] = legend.ToJson("empty (space)"),
                ["rows"] = rows,
            };
        });
    }
}

public sealed class FindTilesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "find_tiles";

    public override string Description =>
        "Finds all tiles of the given tile prototype ids on a grid and returns their coordinates. " +
        "Searches the whole grid by default; pass a rectangle (x,y south-west corner or relative center " +
        "+ width/height) to clip the search.";

    public override JsonObject Annotations => Annotate.ReadOnly();

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["grid"] = Schema.Grid(),
            ["tiles"] = Schema.Array("Tile prototype ids to look for (e.g. [\"FloorSteel\"]).", Schema.String("Tile prototype id.")),
            ["x"] = Schema.Int("Rectangle south-west corner X (omit for whole grid)."),
            ["y"] = Schema.Int("Rectangle south-west corner Y (omit for whole grid)."),
            ["relative"] = Schema.Relative("the center of the rectangle"),
            ["width"] = Schema.Int("Rectangle width in tiles (default 21 when a rectangle is used)."),
            ["height"] = Schema.Int("Rectangle height in tiles (default 21 when a rectangle is used)."),
            ["limit"] = Schema.Int("Max matches to return (default 500)."),
        },
        "tiles");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var limit = McpContext.OptInt(args, "limit") ?? 500;
        if (args["tiles"] is not JsonArray tilesArg || tilesArg.Count == 0)
            throw new McpToolException("'tiles' must be a non-empty array of tile prototype ids.");

        var hasRect = args.ContainsKey("x") || args.ContainsKey("y") || args.ContainsKey("relative") ||
                      args.ContainsKey("width") || args.ContainsKey("height");
        var width = McpContext.OptInt(args, "width") ?? 21;
        var height = McpContext.OptInt(args, "height") ?? 21;
        if (hasRect && (width < 1 || height < 1 || (long) width * height > MaxArea))
            throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var targetIds = new HashSet<int>();
            foreach (var node in tilesArg)
            {
                var name = node?.GetValue<string>() ?? throw new McpToolException("'tiles' entries must be strings.");
                targetIds.Add(TileTools.ResolveTileDef(Ctx, name).TileId);
            }

            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var matches = new JsonArray();
            var truncated = false;

            EntityUid gridUid;
            MapGridComponent grid;
            var result = new JsonObject();

            if (hasRect)
            {
                (gridUid, grid, var anchor) = Ctx.ResolveTilePosition(args);
                var corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;
                result["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y };

                for (var y = corner.Y; y < corner.Y + height && !truncated; y++)
                {
                    for (var x = corner.X; x < corner.X + width; x++)
                    {
                        var tileRef = mapSystem.GetTileRef(gridUid, grid, new Vector2i(x, y));
                        if (!targetIds.Contains(tileRef.Tile.TypeId))
                            continue;
                        if (matches.Count >= limit)
                        {
                            truncated = true;
                            break;
                        }

                        matches.Add(new JsonObject
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["tile"] = TileTools.TileName(Ctx, tileRef.Tile.TypeId),
                        });
                    }
                }
            }
            else
            {
                (gridUid, grid) = Ctx.ResolveGrid(args);
                var enumerator = mapSystem.GetAllTilesEnumerator(gridUid, grid);
                while (enumerator.MoveNext(out var tileRef))
                {
                    if (!targetIds.Contains(tileRef.Value.Tile.TypeId))
                        continue;
                    if (matches.Count >= limit)
                    {
                        truncated = true;
                        break;
                    }

                    matches.Add(new JsonObject
                    {
                        ["x"] = tileRef.Value.GridIndices.X,
                        ["y"] = tileRef.Value.GridIndices.Y,
                        ["tile"] = TileTools.TileName(Ctx, tileRef.Value.Tile.TypeId),
                    });
                }
            }

            result["grid"] = Ctx.ToNetId(gridUid);
            result["matches"] = matches;
            result["truncated"] = truncated;
            return (JsonNode) result;
        });
    }
}

public sealed class ReadAreasTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "read_areas";

    public override string Description =>
        "Reads the RMC-14 area assignment (AreaGridComponent) for a rectangle of tiles as a matrix with a legend " +
        "of area prototype ids. Areas drive minimap labels, weather, mortar/CAS rules etc. " +
        "format 'summary' returns per-area tile counts instead of a matrix — no legend symbol limit — and " +
        "with no rectangle given it covers the WHOLE grid: the cheap way to list every area of a ship. " +
        "Write assignments with paint_areas.";

    public override JsonObject Annotations => Annotate.ReadOnly();

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["grid"] = Schema.Grid(),
        ["x"] = Schema.Int("South-west corner tile X (absolute form)."),
        ["y"] = Schema.Int("South-west corner tile Y (absolute form)."),
        ["relative"] = Schema.Relative("the center of the rectangle"),
        ["width"] = Schema.Int("Rectangle width in tiles (default 21)."),
        ["height"] = Schema.Int("Rectangle height in tiles (default 21)."),
        ["format"] = Schema.String(
            "'matrix' (default): character matrix + legend, at most 70 distinct areas per call. " +
            "'summary': per-area tile counts without a matrix; omit the rectangle to cover the whole grid."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var format = McpContext.OptString(args, "format") ?? "matrix";
        if (format is not ("matrix" or "summary"))
            throw new McpToolException("format must be 'matrix' or 'summary'.");

        var summary = format == "summary";
        var hasRect = args.ContainsKey("x") || args.ContainsKey("y") || args.ContainsKey("relative") ||
                      args.ContainsKey("width") || args.ContainsKey("height");
        var width = McpContext.OptInt(args, "width") ?? 21;
        var height = McpContext.OptInt(args, "height") ?? 21;
        // The area cap protects the per-tile matrix loop; summaries iterate the (bounded)
        // area dictionary instead and need no cap.
        if (!summary && (width < 1 || height < 1 || (long) width * height > MaxArea))
            throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var (gridUid, grid, anchor) = Ctx.ResolveTilePosition(args);
            var corner = args.ContainsKey("relative")
                ? anchor - new Vector2i(width / 2, height / 2)
                : anchor;

            if (!Ctx.EntityManager.TryGetComponent<AreaGridComponent>(gridUid, out var areaGrid))
                throw new McpToolException("This grid has no AreaGridComponent (no RMC areas assigned yet — assign some with paint_areas).");

            var areaSystem = Ctx.EntityManager.System<AreaSystem>();

            if (summary)
            {
                var counts = areaSystem.GetAreaTileCounts(
                    (gridUid, areaGrid),
                    hasRect ? corner : null,
                    hasRect ? new Vector2i(width, height) : null);

                var areas = new JsonObject();
                foreach (var (areaProto, count) in counts.OrderByDescending(kv => kv.Value))
                {
                    areas[areaProto.Id] = count;
                }

                var result = new JsonObject
                {
                    ["grid"] = Ctx.ToNetId(gridUid),
                    ["format"] = "summary",
                    ["distinct_areas"] = counts.Count,
                    ["assigned_tiles"] = counts.Values.Sum(),
                    ["areas"] = areas,
                };
                if (hasRect)
                {
                    result["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y };
                    result["width"] = width;
                    result["height"] = height;
                }
                else
                {
                    result["coverage"] = "whole grid";
                }

                return (JsonNode) result;
            }
            var legend = new McpLegend();
            var rows = new JsonArray();
            for (var y = corner.Y + height - 1; y >= corner.Y; y--)
            {
                var row = new StringBuilder(width);
                for (var x = corner.X; x < corner.X + width; x++)
                {
                    // TryGetAreaProto, not TryGetArea: the latter needs area entities that only
                    // spawn at map-init, and mapping sessions are pre-init by design.
                    row.Append(areaSystem.TryGetAreaProto((gridUid, areaGrid), new Vector2i(x, y), out var areaProto)
                        ? legend.Get(areaProto.Id)
                        : '.');
                }

                rows.Add(row.ToString());
            }

            return new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y },
                ["orientation"] = "world-aligned: top row = north, left column = west",
                ["legend"] = legend.ToJson("no area"),
                ["rows"] = rows,
            };
        });
    }
}

/// <summary>Tile prototype helpers shared by tools.</summary>
public static class TileTools
{
    public static string TileName(McpContext ctx, int typeId)
    {
        var def = ctx.TileDefinitionManager[typeId];
        return def is ContentTileDefinition content ? content.ID : def.Name;
    }

    public static ContentTileDefinition ResolveTileDef(McpContext ctx, string id)
    {
        if (ctx.PrototypeManager.TryIndex<ContentTileDefinition>(id, out var def))
            return def;
        throw new McpToolException($"Unknown tile prototype '{id}' (see list_tile_prototypes).");
    }
}
#endif
