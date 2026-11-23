using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.TacticalMap.Controls;

public enum TacticalMapLabelMode
{
    All = 0,
    Tactical = 1,
    Area = 2,
}

[Flags]
public enum TacticalMapOverlays
{
    None = 0,
    Roof0 = 1 << 0,
    Roof1 = 1 << 1,
    Roof2 = 1 << 2,
    Roof3 = 1 << 3,
    Roof4 = 1 << 4,
    LinkedLz = 1 << 5,
}

public enum TacticalMapRadiusMode
{
    None,
    OrbitalBombardment,
    Mortar,
}

public sealed partial class TacticalMapControl : Control
{
    public const float OverlayUnitsPerTile = 3f;
    public const float MinDragDistance = 10f;

    private const float BaseBlipSize = 14f;
    private const float ClickTolerance = 8f;
    private const float MapTileOpacity = 0.75f;

    private static readonly ResPath MapBlipsRsi = new("/Textures/_RMC14/Interface/map_blips.rsi");
    private static readonly SpriteSpecifier.Rsi BackgroundRsi = new(MapBlipsRsi, "background");
    private static readonly SpriteSpecifier.Rsi DefibbableRsi = new(MapBlipsRsi, "defibbable");
    private static readonly SpriteSpecifier.Rsi Defibbable2Rsi = new(MapBlipsRsi, "defibbable2");
    private static readonly SpriteSpecifier.Rsi Defibbable3Rsi = new(MapBlipsRsi, "defibbable3");
    private static readonly SpriteSpecifier.Rsi Defibbable4Rsi = new(MapBlipsRsi, "defibbable4");
    private static readonly SpriteSpecifier.Rsi UndefibbableRsi = new(MapBlipsRsi, "undefibbable");
    private static readonly SpriteSpecifier.Rsi HiveLeaderRsi = new(MapBlipsRsi, "xenoleader");
    private static readonly SpriteSpecifier.Rsi MortarRsi = new(MapBlipsRsi, "mortar");
    private static readonly ResPath CrtOverlayPath = new("/Textures/_RMC14/Interface/TacticalMap/crt_overlay.png");
    private const string HiveCoreState = "core";
    private const string HivePylonState = "pylon";

    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly AreaSystem _area;
    private readonly TacticalMapSystem _tacticalMapSystem;
    private readonly ShaderInstance _gridShader;
    private readonly ShaderInstance _crtShader;
    private readonly Texture? _crtOverlayTexture;

    private readonly List<TacticalMapBlipView> _blips = new();
    private readonly List<int> _structureBlips = new();
    private readonly List<(Vector2i Tile, Color Color)> _structureTiles = new();
    private readonly List<TacticalMapCanvasLayer> _layers = new();
    private NetEntity? _selfEntity;
    private Vector2i? _selfIndices;
    private TacticalMapBlip? _selfBlip;

    private Vector2i? _mortarCenter;
    private int _mortarMinRange;
    private int _mortarMaxRange;
    private readonly List<Vector2i> _mortarTiles = new();

    private TacticalMapTool? _tool;

    public readonly List<TacticalMapStroke> OverlayStrokes = new();

    public TacticalMapCanvasDraft? Draft { get; private set; }

    public float BlipSizeMultiplier { get; set; } = 0.9f;
    public float BlipAlpha { get; set; } = 1f;
    public bool LabelsEnabled { get; set; } = true;
    public TacticalMapLabelMode LabelMode { get; set; } = TacticalMapLabelMode.All;
    public TacticalMapOverlays Overlays { get; set; }
    public TacticalMapRadiusMode RadiusMode { get; set; }
    public float OrbitalRadius { get; set; } = 17.5f;
    public float MortarRadius { get; set; } = 5.3f;
    public float HiveCoreRange { get; set; } = 11.848f;
    public float HivePylonRange { get; set; } = 8.463f;
    public bool PointMode { get; set; }
    public bool CrtOverlay { get; set; } = true;
    public TacticalMapTheme MapTheme { get; set; } = TacticalMapTheme.Marine;
    public bool AllowBlipClicks { get; set; } = true;
    public Color DrawColor { get; set; } = Color.Black;
    public float DrawThickness { get; set; } = 2f;
    public int LineLimit { get; set; } = 1000;
    public int LabelMaxLength { get; set; } = 64;

    public event Action<NetEntity>? BlipClicked;
    public event Action<NetEntity, Vector2i>? BlipRightClicked;
    public event Action<Vector2i>? PointClicked;
    public event Action<Vector2i, Vector2>? ContextMenuRequested;
    public event Action<Vector2i?>? HoverChanged;
    public event Action? DraftChanged;
    public event Action? UserInteraction;

    public TacticalMapControl()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entMan.System<SpriteSystem>();
        _area = _entMan.System<AreaSystem>();
        _tacticalMapSystem = _entMan.System<TacticalMapSystem>();
        _gridShader = _prototypes.Index<ShaderPrototype>("RMCTacMapGrid").InstanceUnique();
        _crtShader = _prototypes.Index<ShaderPrototype>("RMCTacMapCrt").InstanceUnique();
        if (_resourceCache.TryGetResource<TextureResource>(CrtOverlayPath, out var crtOverlay))
            _crtOverlayTexture = crtOverlay.Texture;
        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;
    }

    public TacticalMapTool? Tool
    {
        get => _tool;
        set
        {
            if (_tool == value)
                return;

            _tool?.Cancel();
            _tool = value;
        }
    }

    public bool IsDraftVisible => Draft != null && (_tool != null || Draft.Dirty);

    public void SetDraft(TacticalMapCanvasDraft? draft)
    {
        if (Draft == draft)
            return;

        _tool?.Cancel();
        Draft = draft;
    }

    public void NotifyDraftChanged()
    {
        DraftChanged?.Invoke();
    }

    public void SetBlips(IReadOnlyList<TacticalMapBlipView> blips)
    {
        _blips.Clear();
        _blips.AddRange(blips);
        RebuildStructureOverlays();
    }

    public void SetSelf(NetEntity? entity, Vector2i? indices)
    {
        _selfEntity = entity;
        _selfIndices = indices;
        _selfBlip = null;
        if (entity == null)
            return;

        foreach (var blip in _blips)
        {
            if (blip.Entity != entity)
                continue;

            _selfBlip = blip.Blip;
            break;
        }
    }

    public Vector2i? SelfIndices => _selfIndices;

    public void SetLayers(IReadOnlyList<TacticalMapCanvasLayer> layers)
    {
        _layers.Clear();
        _layers.AddRange(layers);
    }

    public void SetMortarMarker(Vector2i center, int minRange, int maxRange)
    {
        _mortarCenter = center;
        _mortarMinRange = Math.Max(0, minRange);
        _mortarMaxRange = Math.Max(_mortarMinRange, maxRange);
        RebuildMortarTiles();
    }

    public void ClearMortarMarker()
    {
        _mortarCenter = null;
        _mortarTiles.Clear();
    }

    public bool IsMortarMarkerTile(Vector2i indices)
    {
        return _mortarCenter == indices || _mortarTiles.Contains(indices);
    }

    private void RebuildMortarTiles()
    {
        _mortarTiles.Clear();
        if (_mortarCenter is not { } center || _mortarMaxRange <= 0)
            return;

        var maxSquared = _mortarMaxRange * _mortarMaxRange;
        var minSquared = _mortarMinRange * _mortarMinRange;
        for (var dx = -_mortarMaxRange; dx <= _mortarMaxRange; dx++)
        {
            for (var dy = -_mortarMaxRange; dy <= _mortarMaxRange; dy++)
            {
                var distance = dx * dx + dy * dy;
                if (distance > maxSquared || distance < minSquared)
                    continue;

                var tile = new Vector2i(center.X + dx, center.Y + dy);
                if (TryGetTileFlags(tile, out var packed) &&
                    (TacticalMapChunk.GetEffectiveFlags(packed) & TacticalMapTileFlags.MortarFire) != 0)
                {
                    _mortarTiles.Add(tile);
                }
            }
        }
    }

    private void RebuildStructureOverlays()
    {
        _structureBlips.Clear();
        _structureTiles.Clear();
        for (var i = 0; i < _blips.Count; i++)
        {
            var blip = _blips[i].Blip;
            if (!IsStructureBlip(blip, out var core))
                continue;

            _structureBlips.Add(i);
            var range = core ? HiveCoreRange : HivePylonRange;
            var filter = core ? TacticalMapTileFlags.OrbitalBombard : TacticalMapTileFlags.CAS;
            var color = core ? Color.FromHex("#9B59B6") : Color.FromHex("#4FD2A3");
            var max = (int) MathF.Ceiling(range);
            var rangeSquared = range * range;
            for (var dx = -max; dx <= max; dx++)
            {
                for (var dy = -max; dy <= max; dy++)
                {
                    if (dx * dx + dy * dy > rangeSquared)
                        continue;

                    var tile = new Vector2i(blip.Indices.X + dx, blip.Indices.Y + dy);
                    if (TryGetTileFlags(tile, out var packed) &&
                        (TacticalMapChunk.GetAreaFlags(packed) & filter) != 0)
                    {
                        _structureTiles.Add((tile, color));
                    }
                }
            }
        }
    }

    private static bool IsStructureBlip(TacticalMapBlip blip, out bool core)
    {
        core = false;
        if (blip.Icon.RsiPath != MapBlipsRsi)
            return false;

        if (blip.Icon.RsiState == HiveCoreState)
        {
            core = true;
            return true;
        }

        return blip.Icon.RsiState == HivePylonState;
    }

    public bool TryFindBlip(Vector2 pixel, out TacticalMapBlipView found)
    {
        found = default;
        if (!_hasBase)
            return false;

        var size = GetBlipSize();
        var tolerance = ClickTolerance * GetOverlayScale();
        for (var i = _blips.Count - 1; i >= 0; i--)
        {
            var blip = _blips[i];
            var center = TileToPixel(new Vector2(blip.Blip.Indices.X + 0.5f, blip.Blip.Indices.Y + 0.5f));
            var half = size / 2f + tolerance;
            if (MathF.Abs(pixel.X - center.X) > half || MathF.Abs(pixel.Y - center.Y) > half)
                continue;

            found = blip;
            return true;
        }

        return false;
    }

    public TacticalMapAreaInfo GetAreaInfo(Vector2i indices)
    {
        var name = Loc.GetString("rmc-tacmap-alert-no-area");
        string? areaId = null;
        string? linkedLz = null;
        if (_map is { } map &&
            _entMan.TryGetComponent(map, out AreaGridComponent? areaGrid) &&
            _area.TryGetAreaId(areaGrid, indices, out var area))
        {
            areaId = area.Id;
            if (_prototypes.TryIndex<EntityPrototype>(area.Id, out var proto))
                name = proto.Name;

            linkedLz = _areaLinkedLz.GetValueOrDefault(area.Id);
        }

        TryGetTileFlags(indices, out var packed);
        return new TacticalMapAreaInfo(
            indices,
            name,
            areaId,
            _areaLabels.GetValueOrDefault(indices),
            FindTacticalLabel(indices)?.Text,
            TacticalMapChunk.GetAreaFlags(packed),
            TacticalMapChunk.GetRoofBlocked(packed),
            linkedLz);
    }

    private TacticalMapLabelData? FindTacticalLabel(Vector2i indices)
    {
        if (IsDraftVisible && Draft!.Labels.TryGetValue(indices, out var draftLabel))
            return draftLabel;

        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (IsDraftVisible && layer.Channel == Draft!.Channel)
                continue;

            if (layer.Labels.TryGetValue(indices, out var label))
                return label;
        }

        return null;
    }

    private Texture GetFrame(SpriteSpecifier.Rsi rsi, TimeSpan time)
    {
        return _sprite.GetFrame(rsi, time);
    }
}
