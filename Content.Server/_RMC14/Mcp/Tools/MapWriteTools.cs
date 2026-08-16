#if !FULL_RELEASE || RMC_MCP
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared._RMC14.Areas;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Mcp.Tools;

public sealed class SetTilesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;
    private static readonly Random Random = new();

    public override string Name => "set_tiles";

    public override string Description =>
        "Sets tiles on a grid in one batch. Three forms: " +
        "(1) 'tiles': explicit list of {x,y,tile}; " +
        "(2) 'tile' + rectangle fill (x,y = south-west corner, or relative = center, width/height); " +
        "(3) 'matrix': rows of legend characters, first row = NORTHMOST. IMPORTANT: x,y is the SOUTH-WEST " +
        "corner of the matrix area (same convention as every other rectangle tool), so the FIRST row is " +
        "painted at y + rowCount - 1 and the LAST row lands exactly at y. " +
        "In the matrix, ' ' skips a cell and '.' means the Space tile unless the legend overrides it. " +
        "Tile id 'Space' erases tiles. The result echoes south_west_corner and north_row_y - verify them. " +
        "dry_run=true previews the write: same result plus a histogram of existing tiles that would be " +
        "overwritten, without changing anything — use it to catch a misplaced anchor before painting.";

    public override JsonObject Annotations => Annotate.Write(destructive: true, idempotent: true);

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["grid"] = Schema.Grid(),
        ["tiles"] = Schema.Array("Form 1: explicit tiles.", Schema.Object(new JsonObject
            {
                ["x"] = Schema.Int("Tile X."),
                ["y"] = Schema.Int("Tile Y."),
                ["tile"] = Schema.String("Tile prototype id."),
            },
            "x", "y", "tile")),
        ["tile"] = Schema.String("Form 2: tile prototype id to fill the rectangle with."),
        ["x"] = Schema.Int("SOUTH-WEST corner X of the rectangle or matrix area (absolute form)."),
        ["y"] = Schema.Int("SOUTH-WEST corner Y of the rectangle or matrix area (absolute form; the matrix's LAST row lands here)."),
        ["relative"] = Schema.Relative("the rectangle center / matrix center"),
        ["width"] = Schema.Int("Form 2: fill width in tiles (default 1)."),
        ["height"] = Schema.Int("Form 2: fill height in tiles (default 1)."),
        ["matrix"] = Schema.Object(new JsonObject
            {
                ["legend"] = Schema.Object(new JsonObject()),
                ["rows"] = Schema.Array("Rows of legend characters, first row = northmost.", Schema.String("Row string.")),
            },
            "rows"),
        ["pick_variant"] = Schema.Bool("Randomize visual variants of placed tiles (default true, like variantize)."),
        ["dry_run"] = Schema.Bool("Preview only: report what would change without applying (default false)."),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var pickVariant = McpContext.OptBool(args, "pick_variant", true);
        var dryRun = McpContext.OptBool(args, "dry_run", false);

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var changes = new List<(Vector2i GridIndices, Tile Tile)>();
            EntityUid gridUid;
            MapGridComponent grid;
            Vector2i? reportCorner = null;
            int? reportNorthY = null;

            if (args.TryGetPropertyValue("tiles", out var tilesNode) && tilesNode is JsonArray tilesArray)
            {
                (gridUid, grid) = Ctx.ResolveGrid(args);
                if (tilesArray.Count > MaxArea)
                    throw new McpToolException($"Too many tiles ({tilesArray.Count} > {MaxArea}).");

                foreach (var node in tilesArray)
                {
                    if (node is not JsonObject entry)
                        throw new McpToolException("'tiles' entries must be objects {x, y, tile}.");
                    var pos = new Vector2i(McpContext.GetInt(entry, "x"), McpContext.GetInt(entry, "y"));
                    changes.Add((pos, MakeTile(McpContext.GetString(entry, "tile"), pickVariant)));
                }
            }
            else if (args.TryGetPropertyValue("matrix", out var matrixNode) && matrixNode is JsonObject matrix)
            {
                if (matrix["rows"] is not JsonArray rows || rows.Count == 0)
                    throw new McpToolException("matrix.rows must be a non-empty array of strings.");

                var legend = new Dictionary<char, string>();
                if (matrix.TryGetPropertyValue("legend", out var legendNode) && legendNode is JsonObject legendObj)
                {
                    foreach (var (key, value) in legendObj)
                    {
                        if (key.Length != 1 || value is not JsonValue v || !v.TryGetValue<string>(out var tileId))
                            throw new McpToolException("matrix.legend must map single characters to tile ids.");
                        legend[key[0]] = tileId;
                    }
                }

                var height = rows.Count;
                var width = 0;
                var rowStrings = new List<string>(height);
                foreach (var row in rows)
                {
                    var s = row?.GetValue<string>() ?? throw new McpToolException("matrix.rows entries must be strings.");
                    width = Math.Max(width, s.Length);
                    rowStrings.Add(s);
                }

                if ((long) width * height > MaxArea)
                    throw new McpToolException($"Matrix area {width}x{height} exceeds {MaxArea}.");

                Vector2i corner;
                (gridUid, grid, var anchor) = Ctx.ResolveTilePosition(args);
                corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;
                reportCorner = corner;
                reportNorthY = corner.Y + height - 1;

                for (var rowIndex = 0; rowIndex < rowStrings.Count; rowIndex++)
                {
                    // First row is the northmost one.
                    var y = corner.Y + height - 1 - rowIndex;
                    var s = rowStrings[rowIndex];
                    for (var i = 0; i < s.Length; i++)
                    {
                        var c = s[i];
                        if (c == ' ')
                            continue;

                        string tileId;
                        if (legend.TryGetValue(c, out var mapped))
                            tileId = mapped;
                        else if (c == '.')
                            tileId = "Space";
                        else
                            throw new McpToolException($"Matrix character '{c}' is not in the legend.");

                        changes.Add((new Vector2i(corner.X + i, y), MakeTile(tileId, pickVariant)));
                    }
                }
            }
            else if (McpContext.OptString(args, "tile") is { } fillTile)
            {
                var width = McpContext.OptInt(args, "width") ?? 1;
                var height = McpContext.OptInt(args, "height") ?? 1;
                if (width < 1 || height < 1 || (long) width * height > MaxArea)
                    throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

                (gridUid, grid, var anchor) = Ctx.ResolveTilePosition(args);
                var corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;
                reportCorner = corner;

                for (var y = corner.Y; y < corner.Y + height; y++)
                {
                    for (var x = corner.X; x < corner.X + width; x++)
                    {
                        changes.Add((new Vector2i(x, y), MakeTile(fillTile, pickVariant)));
                    }
                }
            }
            else
            {
                throw new McpToolException("Provide one of: 'tiles', 'matrix', or 'tile' (+rectangle).");
            }

            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();

            var result = new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["tiles_set"] = changes.Count,
            };

            if (reportCorner is { } rc)
            {
                result["south_west_corner"] = new JsonObject { ["x"] = rc.X, ["y"] = rc.Y };
                if (reportNorthY is { } ny)
                    result["north_row_y"] = ny;
            }

            if (dryRun)
            {
                // Histogram of existing non-empty tiles this write would replace with something
                // else — an unexpected entry here usually means a misplaced anchor.
                var overwrites = new Dictionary<string, int>();
                foreach (var (pos, newTile) in changes)
                {
                    var current = mapSystem.GetTileRef(gridUid, grid, pos).Tile;
                    if (current.IsEmpty || current.TypeId == newTile.TypeId)
                        continue;
                    var name = TileTools.TileName(Ctx, current.TypeId);
                    overwrites[name] = overwrites.GetValueOrDefault(name) + 1;
                }

                var overwriteJson = new JsonObject();
                foreach (var (name, count) in overwrites)
                {
                    overwriteJson[name] = count;
                }

                result["dry_run"] = true;
                result["would_overwrite"] = overwriteJson;
                return (JsonNode) result;
            }

            mapSystem.SetTiles(gridUid, grid, changes);
            return (JsonNode) result;
        });
    }

    private Tile MakeTile(string tileId, bool pickVariant)
    {
        var def = TileTools.ResolveTileDef(Ctx, tileId);
        if (def.TileId == 0)
            return Tile.Empty;

        var variant = pickVariant && def.Variants > 1 ? (byte) Random.Next(def.Variants) : (byte) 0;
        return new Tile(def.TileId, variant: variant);
    }
}

public sealed class ReplaceTilesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;
    private static readonly Random Random = new();

    public override string Name => "replace_tiles";

    public override string Description =>
        "Replaces every tile of the given type(s) with another tile, on the whole grid or inside a rectangle. " +
        "Superset of the 'tilereplace' console command. dry_run=true reports the would-be replacement count " +
        "without applying.";

    public override JsonObject Annotations => Annotate.Write(destructive: true, idempotent: true);

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["grid"] = Schema.Grid(),
            ["from"] = Schema.Array("Tile prototype ids to replace.", Schema.String("Tile prototype id.")),
            ["to"] = Schema.String("Replacement tile prototype id."),
            ["x"] = Schema.Int("Optional rectangle south-west corner X."),
            ["y"] = Schema.Int("Optional rectangle south-west corner Y."),
            ["relative"] = Schema.Relative("the rectangle center"),
            ["width"] = Schema.Int("Rectangle width (omit for whole grid)."),
            ["height"] = Schema.Int("Rectangle height (omit for whole grid)."),
            ["pick_variant"] = Schema.Bool("Randomize visual variants of placed tiles (default true)."),
            ["dry_run"] = Schema.Bool("Preview only: report what would change without applying (default false)."),
        },
        "from", "to");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        if (args["from"] is not JsonArray fromArray || fromArray.Count == 0)
            throw new McpToolException("'from' must be a non-empty array of tile prototype ids.");
        var to = McpContext.GetString(args, "to");
        var pickVariant = McpContext.OptBool(args, "pick_variant", true);
        var dryRun = McpContext.OptBool(args, "dry_run", false);
        var width = McpContext.OptInt(args, "width");
        var height = McpContext.OptInt(args, "height");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var fromIds = new HashSet<int>();
            foreach (var node in fromArray)
            {
                var name = node?.GetValue<string>() ?? throw new McpToolException("'from' entries must be strings.");
                fromIds.Add(TileTools.ResolveTileDef(Ctx, name).TileId);
            }

            var toDef = TileTools.ResolveTileDef(Ctx, to);
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var changes = new List<(Vector2i GridIndices, Tile Tile)>();

            EntityUid gridUid;
            MapGridComponent grid;
            if (width != null || height != null)
            {
                var w = width ?? 1;
                var h = height ?? 1;
                if (w < 1 || h < 1 || (long) w * h > MaxArea)
                    throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

                (gridUid, grid, var anchor) = Ctx.ResolveTilePosition(args);
                var corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(w / 2, h / 2)
                    : anchor;

                for (var y = corner.Y; y < corner.Y + h; y++)
                {
                    for (var x = corner.X; x < corner.X + w; x++)
                    {
                        var pos = new Vector2i(x, y);
                        var tileRef = mapSystem.GetTileRef(gridUid, grid, pos);
                        if (fromIds.Contains(tileRef.Tile.TypeId))
                            changes.Add((pos, MakeTile(toDef, pickVariant)));
                    }
                }
            }
            else
            {
                (gridUid, grid) = Ctx.ResolveGrid(args);
                var enumerator = mapSystem.GetAllTilesEnumerator(gridUid, grid, ignoreEmpty: false);
                while (enumerator.MoveNext(out var tileRef))
                {
                    if (fromIds.Contains(tileRef.Value.Tile.TypeId))
                        changes.Add((tileRef.Value.GridIndices, MakeTile(toDef, pickVariant)));
                }
            }

            if (!dryRun)
                mapSystem.SetTiles(gridUid, grid, changes);

            var result = new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["tiles_replaced"] = changes.Count,
            };
            if (dryRun)
                result["dry_run"] = true;
            return (JsonNode) result;
        });
    }

    private static Tile MakeTile(ContentTileDefinition def, bool pickVariant)
    {
        if (def.TileId == 0)
            return Tile.Empty;
        var variant = pickVariant && def.Variants > 1 ? (byte) Random.Next(def.Variants) : (byte) 0;
        return new Tile(def.TileId, variant: variant);
    }
}

public sealed class PaintAreasTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxArea = 60_000;

    public override string Name => "paint_areas";

    public override string Description =>
        "Assigns RMC-14 areas to tiles by writing the grid's AreaGridComponent directly — the per-grid, " +
        "mapping-safe replacement for spawning area markers and running the global 'areas:save'. " +
        "Two forms: rectangle ('area' + x,y south-west corner or relative center + width/height; " +
        "'clear': true removes assignments instead) and 'matrix' (rows of legend characters, first row = " +
        "NORTHMOST, x,y = SOUTH-WEST corner like set_tiles; ' ' skips a cell, '.' clears it). " +
        "Area ids are entity prototypes with an Area component (find them with list_entity_prototypes, " +
        "e.g. search 'CMArea' or 'RMCArea'). Works on uninitialized (mapping) maps; verify with read_areas.";

    public override JsonObject Annotations => Annotate.Write(destructive: true, idempotent: true);

    public override JsonObject InputSchema => Schema.Object(new JsonObject
    {
        ["grid"] = Schema.Grid(),
        ["area"] = Schema.String("Rectangle form: area prototype id to assign."),
        ["clear"] = Schema.Bool("Rectangle form: remove assignments instead of painting (default false)."),
        ["x"] = Schema.Int("SOUTH-WEST corner X of the rectangle or matrix area (absolute form)."),
        ["y"] = Schema.Int("SOUTH-WEST corner Y of the rectangle or matrix area (absolute form)."),
        ["relative"] = Schema.Relative("the rectangle center / matrix center"),
        ["width"] = Schema.Int("Rectangle width in tiles (default 1)."),
        ["height"] = Schema.Int("Rectangle height in tiles (default 1)."),
        ["matrix"] = Schema.Object(new JsonObject
            {
                ["legend"] = Schema.Object(new JsonObject()),
                ["rows"] = Schema.Array("Rows of legend characters, first row = northmost.", Schema.String("Row string.")),
            },
            "rows"),
    });

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            // Tile -> area proto id, null = clear the assignment.
            var changes = new List<(Vector2i Tile, EntProtoId<AreaComponent>? Area)>();
            EntityUid gridUid;
            Vector2i corner;
            int? reportNorthY = null;

            if (args.TryGetPropertyValue("matrix", out var matrixNode) && matrixNode is JsonObject matrix)
            {
                if (matrix["rows"] is not JsonArray rows || rows.Count == 0)
                    throw new McpToolException("matrix.rows must be a non-empty array of strings.");

                var legend = new Dictionary<char, EntProtoId<AreaComponent>>();
                if (matrix.TryGetPropertyValue("legend", out var legendNode) && legendNode is JsonObject legendObj)
                {
                    foreach (var (key, value) in legendObj)
                    {
                        if (key.Length != 1 || value is not JsonValue v || !v.TryGetValue<string>(out var areaId))
                            throw new McpToolException("matrix.legend must map single characters to area prototype ids.");
                        legend[key[0]] = ResolveAreaProto(areaId);
                    }
                }

                var height = rows.Count;
                var width = 0;
                var rowStrings = new List<string>(height);
                foreach (var row in rows)
                {
                    var s = row?.GetValue<string>() ?? throw new McpToolException("matrix.rows entries must be strings.");
                    width = Math.Max(width, s.Length);
                    rowStrings.Add(s);
                }

                if ((long) width * height > MaxArea)
                    throw new McpToolException($"Matrix area {width}x{height} exceeds {MaxArea}.");

                (gridUid, _, var anchor) = Ctx.ResolveTilePosition(args);
                corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;
                reportNorthY = corner.Y + height - 1;

                for (var rowIndex = 0; rowIndex < rowStrings.Count; rowIndex++)
                {
                    // First row is the northmost one, same convention as set_tiles.
                    var y = corner.Y + height - 1 - rowIndex;
                    var s = rowStrings[rowIndex];
                    for (var i = 0; i < s.Length; i++)
                    {
                        var c = s[i];
                        if (c == ' ')
                            continue;

                        if (legend.TryGetValue(c, out var area))
                            changes.Add((new Vector2i(corner.X + i, y), area));
                        else if (c == '.')
                            changes.Add((new Vector2i(corner.X + i, y), null));
                        else
                            throw new McpToolException($"Matrix character '{c}' is not in the legend.");
                    }
                }
            }
            else
            {
                var clear = McpContext.OptBool(args, "clear", false);
                EntProtoId<AreaComponent>? area = null;
                if (!clear)
                {
                    var areaId = McpContext.OptString(args, "area") ??
                                 throw new McpToolException("Provide 'area' (or 'clear': true, or a 'matrix').");
                    area = ResolveAreaProto(areaId);
                }

                var width = McpContext.OptInt(args, "width") ?? 1;
                var height = McpContext.OptInt(args, "height") ?? 1;
                if (width < 1 || height < 1 || (long) width * height > MaxArea)
                    throw new McpToolException($"width/height must be positive with area <= {MaxArea}.");

                (gridUid, _, var anchor) = Ctx.ResolveTilePosition(args);
                corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;

                for (var y = corner.Y; y < corner.Y + height; y++)
                {
                    for (var x = corner.X; x < corner.X + width; x++)
                    {
                        changes.Add((new Vector2i(x, y), area));
                    }
                }
            }

            var areaSystem = Ctx.EntityManager.System<AreaSystem>();
            var areaGrid = Ctx.EntityManager.TryGetComponent<AreaGridComponent>(gridUid, out var existing)
                ? existing
                : null;
            var painted = 0;
            var cleared = 0;
            foreach (var (tile, area) in changes)
            {
                areaSystem.SetAreaProto((gridUid, areaGrid), tile, area);
                if (area == null)
                    cleared++;
                else
                    painted++;
            }

            var result = new JsonObject
            {
                ["grid"] = Ctx.ToNetId(gridUid),
                ["tiles_painted"] = painted,
                ["tiles_cleared"] = cleared,
                ["south_west_corner"] = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y },
            };
            if (reportNorthY is { } ny)
                result["north_row_y"] = ny;
            return (JsonNode) result;
        });
    }

    private EntProtoId<AreaComponent> ResolveAreaProto(string areaId)
    {
        if (!Ctx.PrototypeManager.TryIndex(areaId, out EntityPrototype? proto))
        {
            throw new McpToolException(
                $"Unknown area prototype '{areaId}' (find area ids with list_entity_prototypes, e.g. search 'CMArea').");
        }

        if (!proto.TryGetComponent<AreaComponent>(out _, Ctx.EntityManager.ComponentFactory))
        {
            throw new McpToolException(
                $"'{areaId}' is not an area prototype (it has no Area component). " +
                "Find area ids with list_entity_prototypes, e.g. search 'CMArea'.");
        }

        return new EntProtoId<AreaComponent>(areaId);
    }
}
#endif
