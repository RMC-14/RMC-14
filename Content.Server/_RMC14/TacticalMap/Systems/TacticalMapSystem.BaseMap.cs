using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.TacticalMap;

public sealed partial class TacticalMapSystem
{
    private readonly Dictionary<EntProtoId<AreaComponent>, (TacticalMapTileFlags Flags, bool Exclude)> _areaFlagsBuffer = new();
    private readonly Dictionary<Vector2i, (uint[] Colors, uint[] Flags)> _bakeBuffer = new();
    private readonly List<Vector2i> _chunkRemoveBuffer = new();

    private void InitializeBaseMap()
    {
        SubscribeLocalEvent<TacticalMapComponent, AreaGridRenderedEvent>(OnAreaGridRendered);
        SubscribeLocalEvent<RoofingEntityChangedEvent>(OnRoofingChanged);
    }

    private void OnAreaGridRendered(Entity<TacticalMapComponent> ent, ref AreaGridRenderedEvent args)
    {
        ent.Comp.BaseDirty = true;
    }

    private void OnRoofingChanged(ref RoofingEntityChangedEvent args)
    {
        if (TryComp(args.Roof, out TransformComponent? xform) &&
            xform.GridUid is { } grid &&
            TryComp(grid, out TacticalMapComponent? map))
        {
            map.RoofsDirty = true;
        }
    }

    private void UpdateBaseMaps()
    {
        var query = EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.BaseDirty && !comp.RoofsDirty)
                continue;

            comp.BaseDirty = false;
            comp.RoofsDirty = false;
            BakeMap((uid, comp));
        }
    }

    private void BakeMap(Entity<TacticalMapComponent> map)
    {
        var comp = map.Comp;
        if (!TryComp(map, out AreaGridComponent? areaGrid))
            return;

        _areaFlagsBuffer.Clear();
        foreach (var (areaId, areaEnt) in areaGrid.AreaEntities)
        {
            if (TryComp(areaEnt, out AreaComponent? area))
                _areaFlagsBuffer[areaId] = (GetAreaFlags(area), area.ExcludeFromTacticalMap);
        }

        RecomputeRoofs(map);

        _bakeBuffer.Clear();
        var hasBounds = false;
        var min = Vector2i.Zero;
        var max = Vector2i.Zero;
        foreach (var (tile, color) in areaGrid.Colors)
        {
            var flags = TacticalMapTileFlags.None;
            if (_area.TryGetAreaId(areaGrid, tile, out var areaId) &&
                _areaFlagsBuffer.TryGetValue(areaId, out var info))
            {
                if (info.Exclude)
                    continue;

                flags = info.Flags;
            }

            var chunkIndex = TacticalMapChunk.ToChunk(tile);
            if (!_bakeBuffer.TryGetValue(chunkIndex, out var data))
            {
                data = (new uint[TacticalMapChunk.TileCount], new uint[TacticalMapChunk.TileCount]);
                _bakeBuffer[chunkIndex] = data;
            }

            var index = TacticalMapChunk.GetIndex(tile);
            var packed = PackColor(color);
            data.Colors[index] = packed == 0 ? 1u : packed;
            data.Flags[index] = TacticalMapChunk.PackFlags(flags, comp.RoofBlocked.GetValueOrDefault(tile));

            if (!hasBounds)
            {
                min = tile;
                max = tile;
                hasBounds = true;
            }
            else
            {
                min = Vector2i.ComponentMin(min, tile);
                max = Vector2i.ComponentMax(max, tile);
            }
        }

        var changed = false;
        var tick = _timing.CurTick;

        _chunkRemoveBuffer.Clear();
        foreach (var key in comp.Chunks.Keys)
        {
            if (!_bakeBuffer.ContainsKey(key))
                _chunkRemoveBuffer.Add(key);
        }

        foreach (var key in _chunkRemoveBuffer)
        {
            comp.Chunks.Remove(key);
            changed = true;
        }

        foreach (var (key, data) in _bakeBuffer)
        {
            if (!comp.Chunks.TryGetValue(key, out var chunk))
            {
                chunk = new TacticalMapChunk();
                comp.Chunks[key] = chunk;
            }
            else if (chunk.Colors.AsSpan().SequenceEqual(data.Colors) && chunk.Flags.AsSpan().SequenceEqual(data.Flags))
            {
                continue;
            }

            Array.Copy(data.Colors, chunk.Colors, TacticalMapChunk.TileCount);
            Array.Copy(data.Flags, chunk.Flags, TacticalMapChunk.TileCount);
            chunk.LastUpdate = tick;
            changed = true;
        }

        var labelsChanged = comp.AreaLabels.Count != areaGrid.Labels.Count;
        var labels = new Dictionary<Vector2i, string>(areaGrid.Labels.Count);
        foreach (var (tile, label) in areaGrid.Labels)
        {
            if (_area.TryGetAreaId(areaGrid, tile, out var areaId) &&
                _areaFlagsBuffer.TryGetValue(areaId, out var info) &&
                info.Exclude)
            {
                continue;
            }

            labels[tile] = label;
            if (!comp.AreaLabels.TryGetValue(tile, out var existing) || existing != label)
                labelsChanged = true;
        }

        if (labelsChanged || labels.Count != comp.AreaLabels.Count)
        {
            comp.AreaLabels = labels;
            changed = true;
        }

        if (comp.HasBounds != hasBounds || comp.BoundsMin != min || comp.BoundsMax != max)
        {
            comp.HasBounds = hasBounds;
            comp.BoundsMin = min;
            comp.BoundsMax = max;
            changed = true;
        }

        if (changed)
            Dirty(map);
    }

    private void RecomputeRoofs(Entity<TacticalMapComponent> map)
    {
        var blockedTiles = map.Comp.RoofBlocked;
        blockedTiles.Clear();
        if (!TryComp(map, out MapGridComponent? grid))
            return;

        var roofs = EntityQueryEnumerator<RoofingEntityComponent, TransformComponent>();
        while (roofs.MoveNext(out var uid, out var roof, out var xform))
        {
            if (xform.GridUid != map.Owner || TerminatingOrDeleted(uid))
                continue;

            var blocked = GetRoofBlocked(roof);
            if (blocked == TacticalMapTileFlags.None)
                continue;

            var center = _transform.WithEntityId(xform.Coordinates, map.Owner).Position / grid.TileSize;
            var range = roof.Range / grid.TileSize;
            var rangeSquared = range * range;
            var minX = (int) MathF.Floor(center.X - range);
            var maxX = (int) MathF.Ceiling(center.X + range);
            var minY = (int) MathF.Floor(center.Y - range);
            var maxY = (int) MathF.Ceiling(center.Y + range);
            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var dx = x + 0.5f - center.X;
                    var dy = y + 0.5f - center.Y;
                    if (dx * dx + dy * dy > rangeSquared)
                        continue;

                    var tile = new Vector2i(x, y);
                    blockedTiles[tile] = blockedTiles.GetValueOrDefault(tile) | blocked;
                }
            }
        }
    }

    private static TacticalMapTileFlags GetAreaFlags(AreaComponent area)
    {
        var flags = TacticalMapTileFlags.Area;
        if (area.CAS)
            flags |= TacticalMapTileFlags.CAS;
        if (area.MortarFire)
            flags |= TacticalMapTileFlags.MortarFire;
        if (area.MortarPlacement)
            flags |= TacticalMapTileFlags.MortarPlacement;
        if (area.Lasing)
            flags |= TacticalMapTileFlags.Lase;
        if (area.Medevac)
            flags |= TacticalMapTileFlags.Medevac;
        if (area.Paradropping)
            flags |= TacticalMapTileFlags.Paradrop;
        if (area.OB)
            flags |= TacticalMapTileFlags.OrbitalBombard;
        if (area.SupplyDrop)
            flags |= TacticalMapTileFlags.SupplyDrop;
        if (area.Fulton)
            flags |= TacticalMapTileFlags.Fulton;
        if (area.LandingZone)
            flags |= TacticalMapTileFlags.LandingZone;

        return flags;
    }

    private static TacticalMapTileFlags GetRoofBlocked(RoofingEntityComponent roof)
    {
        var blocked = TacticalMapTileFlags.None;
        if (!roof.CanCAS)
            blocked |= TacticalMapTileFlags.CAS;
        if (!roof.CanMortarFire)
            blocked |= TacticalMapTileFlags.MortarFire;
        if (!roof.CanMortarPlace)
            blocked |= TacticalMapTileFlags.MortarPlacement;
        if (!roof.CanLase)
            blocked |= TacticalMapTileFlags.Lase;
        if (!roof.CanMedevac)
            blocked |= TacticalMapTileFlags.Medevac;
        if (!roof.CanParadrop)
            blocked |= TacticalMapTileFlags.Paradrop;
        if (!roof.CanOrbitalBombard)
            blocked |= TacticalMapTileFlags.OrbitalBombard;
        if (!roof.CanSupplyDrop)
            blocked |= TacticalMapTileFlags.SupplyDrop;
        if (!roof.CanFulton)
            blocked |= TacticalMapTileFlags.Fulton;

        return blocked;
    }
}
