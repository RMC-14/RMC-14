using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    private const int OutlineTiles = 1;
    private const int GlowScale = 2;
    private const int GlowPadding = 4;
    private const int GlowSpread = 1;
    private const int GlowBlurRadius = 3;

    private static readonly Color BackdropColor = Color.Black;
    private static readonly Color GlowColor = Color.FromHex("#c0f7ff");

    private static readonly TextureLoadParameters FilteredTexture = new()
    {
        SampleParameters = new TextureSampleParameters { Filter = true },
        Srgb = true,
        Preload = true,
    };

    private sealed class BaseMapCache
    {
        public TacticalMapComponent Component = default!;
        public int Version;
        public Vector2i Min;
        public int Width;
        public int Height;
        public uint[] Flags = Array.Empty<uint>();
        public bool[] Mask = Array.Empty<bool>();
        public Texture Map = default!;
        public Texture Backdrop = default!;
        public Texture Glow = default!;
        public Texture? LinkedLz;
        public readonly Texture?[] Roofs = new Texture?[TacticalMapRoofLevels.Count];
        public Dictionary<Vector2i, string> AreaLabels = new();
        public Dictionary<string, string> AreaLinkedLz = new();
    }

    private static readonly Dictionary<EntityUid, BaseMapCache> BaseCache = new();

    private EntityUid? _map;
    private BaseMapCache? _base;
    private bool _hasBase;
    private Vector2i _min;
    private int _width;
    private int _height;
    private Dictionary<Vector2i, string> _areaLabels = new();
    private Dictionary<string, string> _areaLinkedLz = new();

    public EntityUid? MapEntity => _map;

    public void SetBaseMap(EntityUid? map)
    {
        if (_map != map)
        {
            _map = map;
            _base = null;
        }

        RefreshBaseMap();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _tacticalMapSystem.BaseMapChanged += OnBaseMapChanged;
        RefreshBaseMap();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _tacticalMapSystem.BaseMapChanged -= OnBaseMapChanged;
    }

    private void OnBaseMapChanged(EntityUid map, bool removed)
    {
        if (removed)
        {
            if (BaseCache.Remove(map, out var cached))
                DisposeCache(cached);

            if (_map == map)
                ClearBase();

            return;
        }

        if (_map == map)
            RefreshBaseMap();
    }

    private void RefreshBaseMap()
    {
        if (_map is not { } map || !_entMan.TryGetComponent(map, out TacticalMapComponent? comp) || !comp.HasBounds)
        {
            if (_base != null || _hasBase)
                ClearBase();

            return;
        }

        if (_base != null && ReferenceEquals(_base.Component, comp) && _base.Version == comp.Version)
            return;

        if (!BaseCache.TryGetValue(map, out var cached) ||
            !ReferenceEquals(cached.Component, comp) ||
            cached.Version != comp.Version)
        {
            PruneBaseCache();
            var rebuilt = BuildBase(map, comp);
            if (BaseCache.TryGetValue(map, out var old))
                DisposeCache(old);

            BaseCache[map] = rebuilt;
            cached = rebuilt;
        }

        ApplyBase(cached);
    }

    private void PruneBaseCache()
    {
        var stale = new List<EntityUid>();
        foreach (var (uid, cached) in BaseCache)
        {
            if (!_entMan.TryGetComponent(uid, out TacticalMapComponent? comp) || !ReferenceEquals(comp, cached.Component))
                stale.Add(uid);
        }

        foreach (var uid in stale)
        {
            if (BaseCache.Remove(uid, out var cached))
                DisposeCache(cached);
        }
    }

    private static void DisposeCache(BaseMapCache cache)
    {
        DisposeTexture(cache.Map);
        DisposeTexture(cache.Backdrop);
        DisposeTexture(cache.Glow);
        DisposeTexture(cache.LinkedLz);
        foreach (var roof in cache.Roofs)
        {
            DisposeTexture(roof);
        }
    }

    private static void DisposeTexture(Texture? texture)
    {
        if (texture is IDisposable disposable)
            disposable.Dispose();
    }

    private void ClearBase()
    {
        _base = null;
        _hasBase = false;
        _width = 0;
        _height = 0;
        _areaLabels = new Dictionary<Vector2i, string>();
        _areaLinkedLz = new Dictionary<string, string>();
    }

    private void ApplyBase(BaseMapCache cached)
    {
        _base = cached;
        _hasBase = cached.Width > 0 && cached.Height > 0;
        _min = cached.Min;
        _width = cached.Width;
        _height = cached.Height;
        _areaLabels = cached.AreaLabels;
        _areaLinkedLz = cached.AreaLinkedLz;
        ApplyPendingView();
        ClampView();
        RebuildStructureOverlays();
        RebuildMortarTiles();
    }

    private BaseMapCache BuildBase(EntityUid map, TacticalMapComponent comp)
    {
        var min = comp.BoundsMin;
        var width = comp.BoundsMax.X - min.X + 1;
        var height = comp.BoundsMax.Y - min.Y + 1;
        var cache = new BaseMapCache
        {
            Component = comp,
            Version = comp.Version,
            Min = min,
            Width = width,
            Height = height,
            Flags = new uint[width * height],
            Mask = new bool[width * height],
            AreaLabels = new Dictionary<Vector2i, string>(comp.AreaLabels),
        };

        var areaLz = new Dictionary<string, string>();
        AreaGridComponent? areaGrid = null;
        if (_entMan.TryGetComponent(map, out areaGrid))
        {
            foreach (var (areaId, areaEnt) in areaGrid.AreaEntities)
            {
                if (_entMan.TryGetComponent(areaEnt, out AreaComponent? area) &&
                    !string.IsNullOrWhiteSpace(area.LinkedLz))
                {
                    areaLz[areaId.Id] = area.LinkedLz;
                }
            }
        }

        cache.AreaLinkedLz = areaLz;

        using var image = new Image<Rgba32>(width, height);
        var roofs = new Image<Rgba32>?[TacticalMapRoofLevels.Count];
        Image<Rgba32>? linkedLz = null;
        try
        {
            foreach (var (chunkIndex, chunk) in comp.Chunks)
            {
                var originX = chunkIndex.X * TacticalMapChunk.Size;
                var originY = chunkIndex.Y * TacticalMapChunk.Size;
                for (var i = 0; i < TacticalMapChunk.TileCount; i++)
                {
                    var packedColor = chunk.Colors[i];
                    if (packedColor == 0)
                        continue;

                    var tile = new Vector2i(originX + i % TacticalMapChunk.Size, originY + i / TacticalMapChunk.Size);
                    var x = tile.X - min.X;
                    var y = height - 1 - (tile.Y - min.Y);
                    if (x < 0 || y < 0 || x >= width || y >= height)
                        continue;

                    image[x, y] = ToRgba(SharedTacticalMapSystem.UnpackColor(packedColor));

                    var index = y * width + x;
                    var flags = chunk.Flags[i];
                    cache.Flags[index] = flags;
                    cache.Mask[index] = true;

                    var level = TacticalMapRoofLevels.GetLevel(TacticalMapChunk.GetEffectiveFlags(flags));
                    if (level >= 0)
                    {
                        roofs[level] ??= new Image<Rgba32>(width, height);
                        roofs[level]![x, y] = ToRgba(TacticalMapRoofLevels.GetOverlayColor(level));
                    }

                    if (areaGrid != null &&
                        _area.TryGetAreaId(areaGrid, tile, out var areaId) &&
                        areaLz.TryGetValue(areaId.Id, out var lz))
                    {
                        linkedLz ??= new Image<Rgba32>(width, height);
                        linkedLz[x, y] = ToRgba(TacticalMapLinkedLz.GetColor(lz));
                    }
                }
            }

            cache.Map = Texture.LoadFromImage(image);
            cache.Backdrop = BuildBackdrop(cache.Mask, width, height);
            cache.Glow = BuildGlow(FillEnclosed(cache.Mask, width, height), width, height);
            if (linkedLz != null)
                cache.LinkedLz = Texture.LoadFromImage(linkedLz);

            for (var level = 0; level < roofs.Length; level++)
            {
                if (roofs[level] is { } roof)
                    cache.Roofs[level] = Texture.LoadFromImage(roof);
            }
        }
        finally
        {
            linkedLz?.Dispose();
            foreach (var roof in roofs)
            {
                roof?.Dispose();
            }
        }

        return cache;
    }

    private static bool[] FillEnclosed(bool[] mask, int width, int height)
    {
        var outside = new bool[mask.Length];
        var queue = new Queue<int>();
        for (var x = 0; x < width; x++)
        {
            Seed(x, 0);
            Seed(x, height - 1);
        }

        for (var y = 0; y < height; y++)
        {
            Seed(0, y);
            Seed(width - 1, y);
        }

        while (queue.TryDequeue(out var index))
        {
            var x = index % width;
            var y = index / width;
            if (x > 0)
                Seed(x - 1, y);

            if (x < width - 1)
                Seed(x + 1, y);

            if (y > 0)
                Seed(x, y - 1);

            if (y < height - 1)
                Seed(x, y + 1);
        }

        var result = new bool[mask.Length];
        for (var i = 0; i < mask.Length; i++)
        {
            result[i] = mask[i] || !outside[i];
        }

        return result;

        void Seed(int x, int y)
        {
            var index = y * width + x;
            if (mask[index] || outside[index])
                return;

            outside[index] = true;
            queue.Enqueue(index);
        }
    }

    private static bool[] Dilate(bool[] mask, int width, int height, int padding, int radius)
    {
        var paddedWidth = width + padding * 2;
        var paddedHeight = height + padding * 2;
        var result = new bool[paddedWidth * paddedHeight];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!mask[y * width + x])
                    continue;

                for (var dy = -radius; dy <= radius; dy++)
                {
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var px = x + padding + dx;
                        var py = y + padding + dy;
                        if (px >= 0 && py >= 0 && px < paddedWidth && py < paddedHeight)
                            result[py * paddedWidth + px] = true;
                    }
                }
            }
        }

        return result;
    }

    private static Texture BuildBackdrop(bool[] mask, int width, int height)
    {
        var outline = Dilate(mask, width, height, OutlineTiles, OutlineTiles);
        var paddedWidth = width + OutlineTiles * 2;
        var paddedHeight = height + OutlineTiles * 2;
        using var image = new Image<Rgba32>(paddedWidth, paddedHeight);
        var color = ToRgba(BackdropColor);
        for (var y = 0; y < paddedHeight; y++)
        {
            for (var x = 0; x < paddedWidth; x++)
            {
                if (outline[y * paddedWidth + x])
                    image[x, y] = color;
            }
        }

        return Texture.LoadFromImage(image);
    }

    private static Texture BuildGlow(bool[] mask, int width, int height)
    {
        var outline = Dilate(mask, width, height, GlowPadding, OutlineTiles);
        var tilesWide = width + GlowPadding * 2;
        var tilesHigh = height + GlowPadding * 2;
        var glowWidth = tilesWide * GlowScale;
        var glowHeight = tilesHigh * GlowScale;
        var alpha = new float[glowWidth * glowHeight];
        for (var ty = 0; ty < tilesHigh; ty++)
        {
            for (var tx = 0; tx < tilesWide; tx++)
            {
                if (!outline[ty * tilesWide + tx])
                    continue;

                for (var sy = -GlowSpread; sy < GlowScale + GlowSpread; sy++)
                {
                    for (var sx = -GlowSpread; sx < GlowScale + GlowSpread; sx++)
                    {
                        var px = tx * GlowScale + sx;
                        var py = ty * GlowScale + sy;
                        if (px >= 0 && py >= 0 && px < glowWidth && py < glowHeight)
                            alpha[py * glowWidth + px] = 1f;
                    }
                }
            }
        }

        var buffer = new float[alpha.Length];
        for (var pass = 0; pass < 2; pass++)
        {
            BoxBlur(alpha, buffer, glowWidth, glowHeight, GlowBlurRadius, true);
            BoxBlur(buffer, alpha, glowWidth, glowHeight, GlowBlurRadius, false);
        }

        using var image = new Image<Rgba32>(glowWidth, glowHeight);
        var color = ToRgba(GlowColor);
        for (var y = 0; y < glowHeight; y++)
        {
            for (var x = 0; x < glowWidth; x++)
            {
                if (outline[y / GlowScale * tilesWide + x / GlowScale])
                    continue;

                var value = alpha[y * glowWidth + x];
                if (value <= 0f)
                    continue;

                image[x, y] = new Rgba32(color.R, color.G, color.B, (byte) (Math.Clamp(value, 0f, 1f) * 255));
            }
        }

        return Texture.LoadFromImage(image, loadParameters: FilteredTexture);
    }

    private static void BoxBlur(float[] source, float[] target, int width, int height, int radius, bool horizontal)
    {
        var length = horizontal ? width : height;
        var lines = horizontal ? height : width;
        var size = radius * 2 + 1;
        for (var line = 0; line < lines; line++)
        {
            var sum = 0f;
            for (var i = -radius; i <= radius; i++)
            {
                sum += Sample(source, width, horizontal, line, i, length);
            }

            for (var i = 0; i < length; i++)
            {
                var index = horizontal ? line * width + i : i * width + line;
                target[index] = sum / size;
                sum += Sample(source, width, horizontal, line, i + radius + 1, length);
                sum -= Sample(source, width, horizontal, line, i - radius, length);
            }
        }
    }

    private static float Sample(float[] source, int width, bool horizontal, int line, int i, int length)
    {
        if (i < 0 || i >= length)
            return 0f;

        return horizontal ? source[line * width + i] : source[i * width + line];
    }

    private static Rgba32 ToRgba(Color color)
    {
        return new Rgba32(color.R, color.G, color.B, color.A);
    }

    public bool IsWithinMap(Vector2i tile)
    {
        var x = tile.X - _min.X;
        var y = tile.Y - _min.Y;
        return _hasBase && x >= 0 && y >= 0 && x < _width && y < _height;
    }

    public bool TryGetTileFlags(Vector2i tile, out uint packed)
    {
        packed = 0;
        if (_base == null || !IsWithinMap(tile))
            return false;

        var x = tile.X - _min.X;
        var y = _height - 1 - (tile.Y - _min.Y);
        var index = y * _width + x;
        if (!_base.Mask[index])
            return false;

        packed = _base.Flags[index];
        return true;
    }
}
