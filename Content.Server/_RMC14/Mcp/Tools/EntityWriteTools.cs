#if !FULL_RELEASE || RMC_MCP
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Content.Server._RMC14.Mcp.Tools;

public sealed class SpawnEntityTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxBatch = 500;

    public override string Name => "spawn_entity";

    public override string Description =>
        "Spawns one entity (or a batch) at a tile, snapped to the tile center — the sandbox spawn panel for agents. " +
        "Position is a tile on a grid (absolute x,y or relative to the player, including in screen frame). " +
        "Optionally sets facing and anchors/unanchors after spawning. Returns the new NetEntity ids.";

    public override JsonObject Annotations => Annotate.Write(destructive: false, idempotent: false);

    public override JsonObject InputSchema
    {
        get
        {
            var single = new JsonObject
            {
                ["prototype"] = Schema.String("Entity prototype id (see list_entity_prototypes)."),
                ["x"] = Schema.Int("Tile X (absolute form)."),
                ["y"] = Schema.Int("Tile Y (absolute form)."),
                ["relative"] = Schema.Relative("the spawn tile"),
                ["facing"] = Schema.String("Facing: 'north'/'south'/'east'/'west' (or degrees as a number string)."),
                ["anchored"] = Schema.Bool("Override anchoring after spawn (default: prototype's own behavior)."),
            };
            var props = new JsonObject
            {
                ["grid"] = Schema.Grid(),
                ["entities"] = Schema.Array("Batch form: list of spawns (same fields as the single form).",
                    Schema.Object((JsonObject) single.DeepClone())),
            };
            foreach (var (key, value) in single)
            {
                props[key] = value!.DeepClone();
            }

            return Schema.Object(props);
        }
    }

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var spawned = new JsonArray();
            if (args.TryGetPropertyValue("entities", out var batchNode) && batchNode is JsonArray batch)
            {
                if (batch.Count > MaxBatch)
                    throw new McpToolException($"Batch too large ({batch.Count} > {MaxBatch}).");

                foreach (var node in batch)
                {
                    if (node is not JsonObject entry)
                        throw new McpToolException("'entities' entries must be objects.");
                    // Inherit the grid from the top-level arguments unless overridden.
                    if (!entry.ContainsKey("grid") && args.ContainsKey("grid"))
                        entry["grid"] = args["grid"]!.DeepClone();
                    spawned.Add(SpawnOne(entry));
                }
            }
            else
            {
                spawned.Add(SpawnOne(args));
            }

            return (JsonNode) new JsonObject { ["spawned"] = spawned };
        });
    }

    private JsonObject SpawnOne(JsonObject args)
    {
        var prototype = McpContext.GetString(args, "prototype");
        if (!Ctx.PrototypeManager.HasIndex<Robust.Shared.Prototypes.EntityPrototype>(prototype))
            throw new McpToolException($"Unknown entity prototype '{prototype}' (see list_entity_prototypes).");

        var (gridUid, grid, tile) = Ctx.ResolveTilePosition(args);
        var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
        var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
        var coords = mapSystem.GridTileToLocal(gridUid, grid, tile);

        var uid = Ctx.EntityManager.SpawnEntity(prototype, coords);

        if (ParseFacing(args) is { } angle)
            transformSystem.SetLocalRotation(uid, angle);

        var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
        if (args.TryGetPropertyValue("anchored", out _) &&
            McpContext.OptBool(args, "anchored", xform.Anchored) is var anchored && anchored != xform.Anchored)
        {
            if (anchored)
                transformSystem.AnchorEntity((uid, xform));
            else
                transformSystem.Unanchor(uid, xform);
        }

        return new JsonObject
        {
            ["entity"] = Ctx.ToNetId(uid),
            ["prototype"] = prototype,
            ["x"] = tile.X,
            ["y"] = tile.Y,
            ["anchored"] = xform.Anchored,
        };
    }

    internal static Angle? ParseFacing(JsonObject args)
    {
        if (McpContext.OptString(args, "facing") is not { } facing)
            return null;

        if (double.TryParse(facing, out var degrees))
            return Angle.FromDegrees(degrees);

        return facing.ToLowerInvariant() switch
        {
            "south" => Angle.FromDegrees(0),
            "east" => Angle.FromDegrees(90),
            "north" => Angle.FromDegrees(180),
            "west" => Angle.FromDegrees(270),
            _ => throw new McpToolException("facing must be north/south/east/west or a number in degrees."),
        };
    }
}

public sealed class DeleteEntitiesTool(McpContext ctx) : McpTool(ctx)
{
    private const int MaxListed = 100;

    public override string Name => "delete_entities";

    public override string Description =>
        "Deletes entities by NetEntity ids, or every entity matching filters inside a rectangle. " +
        "Never deletes grids, maps or player-controlled entities. Deleting an area WITHOUT any filter removes " +
        "all prototype-spawned entities there (like the eraser), so filter when you only mean one kind. " +
        "dry_run=true previews the doomed entities without deleting anything.";

    public override JsonObject Annotations => Annotate.Write(destructive: true, idempotent: true);

    public override JsonObject InputSchema
    {
        get
        {
            var props = new JsonObject
            {
                ["dry_run"] = Schema.Bool("Preview only: list what would be deleted without deleting (default false)."),
                ["entities"] = Schema.Array("Explicit NetEntity ids to delete.", Schema.Int("NetEntity id.")),
                ["grid"] = Schema.Grid(),
                ["x"] = Schema.Int("Area south-west corner tile X."),
                ["y"] = Schema.Int("Area south-west corner tile Y."),
                ["relative"] = Schema.Relative("the area center"),
                ["width"] = Schema.Int("Area width in tiles."),
                ["height"] = Schema.Int("Area height in tiles."),
            };
            foreach (var (key, value) in EntityFilter.SchemaProperties())
            {
                props[key] = value!.DeepClone();
            }

            return Schema.Object(props);
        }
    }

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var toDelete = new List<EntityUid>();

            if (args.TryGetPropertyValue("entities", out var idsNode) && idsNode is JsonArray ids)
            {
                foreach (var node in ids)
                {
                    if (node is not JsonValue value || !value.TryGetValue<int>(out var netId))
                        throw new McpToolException("'entities' entries must be integers.");
                    toDelete.Add(Ctx.FromNetId(netId));
                }
            }
            else if (McpContext.OptInt(args, "width") is { } width && McpContext.OptInt(args, "height") is { } height)
            {
                if (width < 1 || height < 1)
                    throw new McpToolException("width/height must be positive.");

                var filter = EntityFilter.Parse(Ctx, args);
                var (gridUid, grid, anchor) = Ctx.ResolveTilePosition(args);
                var corner = args.ContainsKey("relative")
                    ? anchor - new Vector2i(width / 2, height / 2)
                    : anchor;

                var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
                var found = new HashSet<EntityUid>();

                // Anchored entities: walk the per-tile lists — exact tile coverage, no AABB slop.
                for (var ty = corner.Y; ty < corner.Y + height; ty++)
                {
                    for (var tx = corner.X; tx < corner.X + width; tx++)
                    {
                        var anchoredEnum = mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, new Vector2i(tx, ty));
                        while (anchoredEnum.MoveNext(out var anchoredUid))
                        {
                            found.Add(anchoredUid.Value);
                        }
                    }
                }

                // Loose entities: physics lookup, then keep only those whose tile is inside the
                // rectangle — the AABB query alone also returns entities merely touching its edge.
                var lookup = Ctx.EntityManager.System<EntityLookupSystem>();
                var intersecting = new HashSet<EntityUid>();
                lookup.GetLocalEntitiesIntersecting(gridUid,
                    new Box2(corner.X, corner.Y, corner.X + width, corner.Y + height), intersecting);
                foreach (var uid in intersecting)
                {
                    var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
                    if (xform.Anchored)
                        continue;

                    var tile = mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
                    if (tile.X >= corner.X && tile.X < corner.X + width &&
                        tile.Y >= corner.Y && tile.Y < corner.Y + height)
                    {
                        found.Add(uid);
                    }
                }

                foreach (var uid in found)
                {
                    var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
                    var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);
                    if (meta.EntityPrototype == null)
                        continue;
                    if (filter.Matches(Ctx, uid, meta, xform))
                        toDelete.Add(uid);
                }
            }
            else
            {
                throw new McpToolException("Provide 'entities' or an area (width+height with x/y or relative).");
            }

            var dryRun = McpContext.OptBool(args, "dry_run", false);
            var deleted = new JsonArray();
            var deletedCount = 0;
            foreach (var uid in toDelete)
            {
                if (!Ctx.EntityManager.EntityExists(uid))
                    continue;
                // Never delete grids, maps or player avatars this way.
                if (Ctx.EntityManager.HasComponent<MapGridComponent>(uid) ||
                    Ctx.EntityManager.HasComponent<MapComponent>(uid) ||
                    Ctx.EntityManager.HasComponent<ActorComponent>(uid))
                {
                    continue;
                }

                if (deleted.Count < MaxListed)
                {
                    deleted.Add(new JsonObject
                    {
                        ["entity"] = Ctx.ToNetId(uid),
                        ["prototype"] = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID,
                    });
                }

                if (!dryRun)
                    Ctx.EntityManager.DeleteEntity(uid);
                deletedCount++;
            }

            var result = new JsonObject
            {
                ["deleted_count"] = deletedCount,
                ["deleted"] = deleted,
            };
            if (dryRun)
                result["dry_run"] = true;
            return (JsonNode) result;
        });
    }
}

public sealed class TransformEntityTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "transform_entity";

    public override string Description =>
        "Moves, rotates, anchors or unanchors an existing entity. Position (if given) snaps to the tile center " +
        "of the target grid; rotation/anchoring can be changed independently.";

    public override JsonObject Annotations => Annotate.Write(destructive: false, idempotent: true);

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["entity"] = Schema.Int("Entity NetEntity id."),
            ["grid"] = Schema.Grid(),
            ["x"] = Schema.Int("Target tile X (absolute form)."),
            ["y"] = Schema.Int("Target tile Y (absolute form)."),
            ["relative"] = Schema.Relative("the target tile"),
            ["facing"] = Schema.String("New facing: 'north'/'south'/'east'/'west' or degrees."),
            ["anchored"] = Schema.Bool("Anchor (true) or unanchor (false)."),
        },
        "entity");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var netId = McpContext.GetInt(args, "entity");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var uid = Ctx.FromNetId(netId);
            var transformSystem = Ctx.EntityManager.System<SharedTransformSystem>();
            var mapSystem = Ctx.EntityManager.System<SharedMapSystem>();
            var xform = Ctx.EntityManager.GetComponent<TransformComponent>(uid);

            var moved = false;
            if (args.ContainsKey("x") || args.ContainsKey("relative"))
            {
                var (gridUid, grid, tile) = Ctx.ResolveTilePosition(args);
                var wasAnchored = xform.Anchored;
                if (wasAnchored)
                    transformSystem.Unanchor(uid, xform);
                transformSystem.SetCoordinates(uid, mapSystem.GridTileToLocal(gridUid, grid, tile));
                if (wasAnchored)
                    transformSystem.AnchorEntity((uid, xform));
                moved = true;
            }

            if (SpawnEntityTool.ParseFacing(args) is { } angle)
                transformSystem.SetLocalRotation(uid, angle);

            if (args.ContainsKey("anchored"))
            {
                var anchored = McpContext.OptBool(args, "anchored", xform.Anchored);
                if (anchored && !xform.Anchored)
                {
                    if (!transformSystem.AnchorEntity((uid, xform)))
                        throw new McpToolException("Failed to anchor (no grid under the entity?).");
                }
                else if (!anchored && xform.Anchored)
                {
                    transformSystem.Unanchor(uid, xform);
                }
            }

            var meta = Ctx.EntityManager.GetComponent<MetaDataComponent>(uid);
            var result = EntityTools.Describe(Ctx, uid, meta, xform);
            result["moved"] = moved;
            return (JsonNode) result;
        });
    }
}

public sealed class SetComponentFieldTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "set_component_field";

    public override string Description =>
        "Writes one field of an entity's component and echoes the new value back — the validated replacement " +
        "for 'vvwrite' (which fails SILENTLY on a wrong path). Read the available fields first with " +
        "entity_info's component dump. Values use YAML/prototype syntax as a string: numbers ('5', '0.4'), " +
        "booleans ('true'), strings (ladder pair ids), colors ('#FF0000'), vectors ('1,2'), prototype ids, " +
        "lists ('[a, b]'); 'null' clears nullable fields. EntityUid/NetEntity fields take a NetEntity id. " +
        "Typical uses: Ladder.Id for ladder pairs, RMCTeleporter.Adjust for trigger teleporters, camera ids.";

    public override JsonObject Annotations => Annotate.Write(destructive: true, idempotent: true);

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["entity"] = Schema.Int("Entity NetEntity id."),
            ["component"] = Schema.String("Component name (as listed by entity_info), e.g. 'Ladder'."),
            ["field"] = Schema.String("Field or property name inside the component, e.g. 'Id'."),
            ["value"] = Schema.String("New value in YAML/prototype syntax; 'null' clears nullable fields."),
        },
        "entity", "component", "field", "value");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var netId = McpContext.GetInt(args, "entity");
        var componentName = McpContext.GetString(args, "component");
        var fieldName = McpContext.GetString(args, "field");
        var rawValue = McpContext.GetString(args, "value");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            var uid = Ctx.FromNetId(netId);
            var registration = Ctx.ResolveComponent(componentName);
            if (!Ctx.EntityManager.TryGetComponent(uid, registration.Type, out var component))
                throw new McpToolException(
                    $"Entity {netId} has no component '{registration.Name}' (entity_info lists its components).");

            var member = ResolveMember(component.GetType(), fieldName);
            var memberType = member switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => throw new McpToolException("Unsupported member kind."),
            };

            var value = ParseValue(memberType, rawValue);

            try
            {
                switch (member)
                {
                    case FieldInfo f:
                        f.SetValue(component, value);
                        break;
                    case PropertyInfo p:
                        p.SetValue(component, value);
                        break;
                }
            }
            catch (Exception e)
            {
                throw new McpToolException($"Failed to set {registration.Name}.{member.Name}: {e.Message}");
            }

            // Replicate to clients (and mark the entity modified) if the component is networked.
            var networked = registration.NetID != null;
            if (networked)
                Ctx.EntityManager.Dirty(uid, component);

            // Read the value back through reflection so the agent sees what was actually stored.
            var readBack = member switch
            {
                FieldInfo f => f.GetValue(component),
                PropertyInfo p => p.GetValue(component),
                _ => null,
            };

            return (JsonNode) new JsonObject
            {
                ["entity"] = netId,
                ["component"] = registration.Name,
                ["field"] = member.Name,
                ["new_value"] = readBack?.ToString(),
                ["networked"] = networked,
            };
        });
    }

    private static MemberInfo ResolveMember(Type type, string fieldName)
    {
        var members = new List<MemberInfo>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            members.Add(field);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0)
                members.Add(property);
        }

        var member = members.FirstOrDefault(m => string.Equals(m.Name, fieldName, StringComparison.Ordinal)) ??
                     members.FirstOrDefault(m => string.Equals(m.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (member == null)
        {
            var writable = members.Where(IsWritable).Select(m => m.Name).ToList();
            var similar = writable.Where(n => n.Contains(fieldName, StringComparison.OrdinalIgnoreCase)).Take(8).ToList();
            var hint = similar.Count > 0
                ? $"Similar fields: {string.Join(", ", similar)}."
                : $"Writable fields: {string.Join(", ", writable.Take(20))}.";
            throw new McpToolException($"Component '{type.Name}' has no field '{fieldName}'. {hint}");
        }

        if (!IsWritable(member))
            throw new McpToolException($"'{type.Name}.{member.Name}' is read-only.");

        return member;
    }

    private static bool IsWritable(MemberInfo member) => member switch
    {
        FieldInfo f => !f.IsInitOnly,
        PropertyInfo p => p.GetSetMethod(nonPublic: true) != null,
        _ => false,
    };

    private object? ParseValue(Type memberType, string rawValue)
    {
        var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;

        if (rawValue == "null")
        {
            if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
                throw new McpToolException($"'{memberType.Name}' is not nullable; provide a value.");
            return null;
        }

        // Entity references are session-local and not data-serializable — take NetEntity ids.
        if (underlying == typeof(EntityUid))
        {
            if (!int.TryParse(rawValue, out var refNetId))
                throw new McpToolException("EntityUid fields take a NetEntity id (integer).");
            return Ctx.FromNetId(refNetId);
        }

        if (underlying == typeof(NetEntity))
        {
            if (!int.TryParse(rawValue, out var refNetId))
                throw new McpToolException("NetEntity fields take a NetEntity id (integer).");
            return new NetEntity(refNetId);
        }

        try
        {
            // The same serializer the prototype YAML goes through. Structured values ([a, b],
            // {x: 1}, multi-line) are parsed as YAML; plain scalars skip the YAML parser so that
            // '#FF0000' does not turn into a comment.
            var trimmed = rawValue.TrimStart();
            DataNode node;
            if (trimmed.StartsWith('[') || trimmed.StartsWith('{') || rawValue.Contains('\n'))
            {
                var yamlStream = new YamlStream();
                yamlStream.Load(new StringReader(rawValue));
                node = yamlStream.Documents[0].RootNode.ToDataNode();
            }
            else
            {
                node = new ValueDataNode(rawValue);
            }

            return Ctx.Serialization.Read(underlying, node);
        }
        catch (Exception e)
        {
            throw new McpToolException(
                $"Cannot parse '{rawValue}' as {underlying.Name}: {e.Message} " +
                "(use YAML/prototype syntax, e.g. '5', 'true', '1,2', '#FF0000', '[a, b]').");
        }
    }
}
#endif
