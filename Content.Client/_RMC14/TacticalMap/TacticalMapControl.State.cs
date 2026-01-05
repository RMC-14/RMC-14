using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._RMC14.TacticalMap;

public sealed partial class TacticalMapControl
{
    private sealed class CachedMapTexture
    {
        public Dictionary<Vector2i, Color>? Colors;
        public Dictionary<Vector2i, EntProtoId<AreaComponent>>? Areas;
        public Dictionary<EntProtoId<AreaComponent>, EntityUid>? AreaEntities;
        public Dictionary<Vector2i, string>? Labels;
        public bool HasTacMapBounds;
        public Vector2i TacMapBoundsMin;
        public Vector2i TacMapBoundsMax;
        public int ColorsCount;
        public int LabelsCount;
        public int AreasCount;
        public int AreaEntitiesCount;
        public Texture? BackgroundTexture;
        public Texture? MapTexture;
        public Texture? LinkedLzOverlay;
        public Dictionary<string, Texture> IconOverlays = new();
        public bool[]? TileMask;
        public int TileMaskWidth;
        public int TileMaskHeight;
        public Vector2i Min;
        public Vector2i Delta;
        public Dictionary<Vector2i, string> AreaLabels = new();
    }

    private static readonly Dictionary<EntityUid, CachedMapTexture> _textureCache = new();

    public void SetCurrentMap(EntityUid? mapEntity)
    {
        _currentMapEntity = mapEntity;
    }

    public void SetCurrentMapName(string? mapName)
    {
        _currentMapName = mapName;
    }

    public void UpdateTexture(Entity<AreaGridComponent> grid)
    {
        _currentAreaGridEntity = grid.Owner;

        if (TryApplyCachedTexture(grid))
            return;

        if (grid.Comp.Colors.Count == 0)
        {
            _tileMask = null;
            _tileMaskWidth = 0;
            _tileMaskHeight = 0;
            _backgroundTexture = null;
            _overlayLinkedLzTexture = null;
            _overlayIconTextures.Clear();
            return;
        }

        var colors = grid.Comp.Colors;
        var hasValue = false;
        Vector2i fullMin = default;
        Vector2i fullMax = default;
        foreach (var (pos, _) in colors)
        {
            if (!hasValue)
            {
                fullMin = pos;
                fullMax = pos;
                hasValue = true;
            }
            else
            {
                fullMin = Vector2i.ComponentMin(fullMin, pos);
                fullMax = Vector2i.ComponentMax(fullMax, pos);
            }
        }

        if (!hasValue)
        {
            _tileMask = null;
            _tileMaskWidth = 0;
            _tileMaskHeight = 0;
            _backgroundTexture = null;
            _overlayLinkedLzTexture = null;
            _overlayIconTextures.Clear();
            return;
        }

        Vector2i boundsMin;
        Vector2i boundsMax;
        if (grid.Comp.HasTacMapBounds)
        {
            boundsMin = Vector2i.ComponentMax(fullMin, grid.Comp.TacMapBoundsMin);
            boundsMax = Vector2i.ComponentMin(fullMax, grid.Comp.TacMapBoundsMax);

            if (boundsMax.X < boundsMin.X || boundsMax.Y < boundsMin.Y)
            {
                boundsMin = fullMin;
                boundsMax = fullMax;
            }
        }
        else
        {
            boundsMin = fullMin;
            boundsMax = fullMax;
        }

        _min = boundsMin;
        _delta = boundsMax - boundsMin;

        if (_delta.X <= 0 || _delta.Y <= 0)
        {
            _tileMask = null;
            _tileMaskWidth = 0;
            _tileMaskHeight = 0;
            _backgroundTexture = null;
            _overlayLinkedLzTexture = null;
            _overlayIconTextures.Clear();
            return;
        }

        int width = _delta.X + 1;
        int height = _delta.Y + 1;
        _tileMaskWidth = width;
        _tileMaskHeight = height;
        _tileMask = new bool[width * height];

        Image<Rgba32> image = new(width, height);
        Image<Rgba32> background = new(width, height);
        Image<Rgba32>? linkedLzOverlay = null;
        var iconOverlays = new Dictionary<string, Image<Rgba32>>();
        bool canLookupAreas = _entitySystemManager.TryGetEntitySystem(out SharedAreaLookupSystem? areaLookupSystem);
        bool hasLinkedLzOverlay = false;
        foreach ((Vector2i position, Color color) in colors)
        {
            if (position.X < boundsMin.X || position.X > boundsMax.X ||
                position.Y < boundsMin.Y || position.Y > boundsMax.Y)
            {
                continue;
            }

            (int x, int y) = GetDrawPosition(position);
            _tileMask[y * width + x] = true;
            var backgroundColor = color.WithAlpha(1f);
            var overlayColor = color.WithAlpha(color.A * MapTileOpacity);
            background[x, y] = new Rgba32(backgroundColor.R, backgroundColor.G, backgroundColor.B, backgroundColor.A);
            image[x, y] = new Rgba32(overlayColor.R, overlayColor.G, overlayColor.B, overlayColor.A);

            if (canLookupAreas && areaLookupSystem != null &&
                areaLookupSystem.TryGetArea(grid.Owner, position, out var area, out _))
            {
                var areaComp = area.Value.Comp;
                string? linkedLz = areaComp.LinkedLz;
                if (!string.IsNullOrWhiteSpace(linkedLz))
                {
                    var lzIds = ParseLinkedLzIds(linkedLz);
                    if (lzIds.Length > 0)
                    {
                        linkedLzOverlay ??= new Image<Rgba32>(width, height);
                        Color lzColor = BlendLinkedLzColors(lzIds);
                        linkedLzOverlay[x, y] = new Rgba32(lzColor.R, lzColor.G, lzColor.B, lzColor.A);
                        hasLinkedLzOverlay = true;
                    }
                }

                string iconState = GetAreaIconState(areaComp);
                if (!string.IsNullOrWhiteSpace(iconState))
                {
                    if (!iconOverlays.TryGetValue(iconState, out var iconOverlay))
                    {
                        iconOverlay = new Image<Rgba32>(width, height);
                        iconOverlays[iconState] = iconOverlay;
                    }

                    Color iconColor = GetAreaIconOverlayColor(iconState);
                    iconOverlay[x, y] = new Rgba32(iconColor.R, iconColor.G, iconColor.B, iconColor.A);
                }
            }
        }

        _backgroundTexture = Texture.LoadFromImage(background);
        var mapTexture = Texture.LoadFromImage(image);
        Texture = mapTexture;
        _overlayLinkedLzTexture = hasLinkedLzOverlay && linkedLzOverlay != null
            ? Texture.LoadFromImage(linkedLzOverlay)
            : null;
        _overlayIconTextures.Clear();
        var overlayTextures = new Dictionary<string, Texture>(iconOverlays.Count);
        foreach (var (state, iconOverlay) in iconOverlays)
        {
            var overlayTexture = Texture.LoadFromImage(iconOverlay);
            _overlayIconTextures[state] = overlayTexture;
            overlayTextures[state] = overlayTexture;
        }
        var areaLabels = new Dictionary<Vector2i, string>();
        foreach ((Vector2i position, string label) in grid.Comp.Labels)
        {
            if (position.X < boundsMin.X || position.X > boundsMax.X ||
                position.Y < boundsMin.Y || position.Y > boundsMax.Y)
            {
                continue;
            }

            areaLabels[position] = label;
        }
        _areaLabels = areaLabels;

        CacheTexture(grid, new CachedMapTexture
        {
            Colors = grid.Comp.Colors,
            Areas = grid.Comp.Areas,
            AreaEntities = grid.Comp.AreaEntities,
            Labels = grid.Comp.Labels,
            HasTacMapBounds = grid.Comp.HasTacMapBounds,
            TacMapBoundsMin = grid.Comp.TacMapBoundsMin,
            TacMapBoundsMax = grid.Comp.TacMapBoundsMax,
            ColorsCount = grid.Comp.Colors.Count,
            LabelsCount = grid.Comp.Labels.Count,
            AreasCount = grid.Comp.Areas.Count,
            AreaEntitiesCount = grid.Comp.AreaEntities.Count,
            BackgroundTexture = _backgroundTexture,
            MapTexture = mapTexture,
            LinkedLzOverlay = _overlayLinkedLzTexture,
            IconOverlays = overlayTextures,
            TileMask = _tileMask,
            TileMaskWidth = _tileMaskWidth,
            TileMaskHeight = _tileMaskHeight,
            Min = _min,
            Delta = _delta,
            AreaLabels = areaLabels
        });

        ApplyViewSettings();
        if (_mortarOverlayCenter != null)
            RebuildMortarOverlayTiles();
    }

    private bool TryApplyCachedTexture(Entity<AreaGridComponent> grid)
    {
        if (!_textureCache.TryGetValue(grid.Owner, out var cached))
            return false;

        if (!IsCacheValid(cached, grid.Comp))
        {
            _textureCache.Remove(grid.Owner);
            return false;
        }

        _min = cached.Min;
        _delta = cached.Delta;
        _tileMask = cached.TileMask;
        _tileMaskWidth = cached.TileMaskWidth;
        _tileMaskHeight = cached.TileMaskHeight;
        _backgroundTexture = cached.BackgroundTexture;
        Texture = cached.MapTexture;
        _overlayLinkedLzTexture = cached.LinkedLzOverlay;
        _overlayIconTextures.Clear();
        foreach (var (state, texture) in cached.IconOverlays)
        {
            _overlayIconTextures[state] = texture;
        }
        _areaLabels = cached.AreaLabels;

        ApplyViewSettings();
        if (_mortarOverlayCenter != null)
            RebuildMortarOverlayTiles();
        return true;
    }

    private static bool IsCacheValid(CachedMapTexture cached, AreaGridComponent grid)
    {
        return ReferenceEquals(cached.Colors, grid.Colors) &&
               ReferenceEquals(cached.Areas, grid.Areas) &&
               ReferenceEquals(cached.AreaEntities, grid.AreaEntities) &&
               ReferenceEquals(cached.Labels, grid.Labels) &&
               cached.HasTacMapBounds == grid.HasTacMapBounds &&
               cached.TacMapBoundsMin == grid.TacMapBoundsMin &&
               cached.TacMapBoundsMax == grid.TacMapBoundsMax &&
               cached.ColorsCount == grid.Colors.Count &&
               cached.LabelsCount == grid.Labels.Count &&
               cached.AreasCount == grid.Areas.Count &&
               cached.AreaEntitiesCount == grid.AreaEntities.Count;
    }

    private static void CacheTexture(Entity<AreaGridComponent> grid, CachedMapTexture cached)
    {
        _textureCache[grid.Owner] = cached;
    }

    private static Color GetLinkedLzOverlayColor(string linkedLz)
    {
        return linkedLz switch
        {
            "dropship_lz1" => Color.FromHex("#4FC3FF").WithAlpha(0.28f),
            "dropship_lz2" => Color.FromHex("#7CFF7A").WithAlpha(0.28f),
            "dropship_lz3" => Color.FromHex("#FFB74D").WithAlpha(0.28f),
            "dropship_lz4" => Color.FromHex("#FF6B6B").WithAlpha(0.28f),
            _ => GetHashedOverlayColor(linkedLz)
        };
    }

    private static string[] ParseLinkedLzIds(string linkedLz)
    {
        var parts = linkedLz.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return Array.Empty<string>();

        var ids = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                ids.Add(trimmed);
        }

        return ids.Count == 0 ? Array.Empty<string>() : ids.ToArray();
    }

    private static Color BlendLinkedLzColors(IReadOnlyList<string> linkedLzIds)
    {
        float r = 0f;
        float g = 0f;
        float b = 0f;
        float maxAlpha = 0f;
        int count = 0;

        foreach (var id in linkedLzIds)
        {
            var color = GetLinkedLzOverlayColor(id);
            r += color.R;
            g += color.G;
            b += color.B;
            maxAlpha = MathF.Max(maxAlpha, color.A);
            count++;
        }

        if (count == 0)
            return Color.Transparent;

        float blendAlpha = maxAlpha;
        if (count > 1)
            blendAlpha = MathF.Min(0.4f, maxAlpha + 0.06f * (count - 1));

        return new Color(r / count, g / count, b / count, blendAlpha);
    }

    private static Color GetAreaIconOverlayColor(string state)
    {
        return state switch
        {
            "roof0" => Color.FromHex("#3DBF75").WithAlpha(0.24f),
            "roof1" => Color.FromHex("#A6D96A").WithAlpha(0.24f),
            "roof2" => Color.FromHex("#F4B860").WithAlpha(0.24f),
            "roof3" => Color.FromHex("#F28C7A").WithAlpha(0.24f),
            "roof4" => Color.FromHex("#6A61FF").WithAlpha(0.24f),
            _ => Color.FromHex("#6A61FF").WithAlpha(0.24f)
        };
    }

    private static string GetAreaIconState(AreaComponent areaComp)
    {
        if (!areaComp.OB)
            return "roof4";

        if (!areaComp.CAS)
            return "roof3";

        if (!areaComp.SupplyDrop || !areaComp.MortarFire)
            return "roof2";

        if (!areaComp.MortarPlacement || !areaComp.Lasing || !areaComp.Medevac || !areaComp.Paradropping)
            return "roof1";

        return "roof0";
    }

    private static Color GetHashedOverlayColor(string key)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in key)
            {
                hash ^= c;
                hash *= 16777619;
            }

            byte r = (byte)(90 + (hash & 0x3F));
            byte g = (byte)(120 + ((hash >> 6) & 0x3F));
            byte b = (byte)(160 + ((hash >> 12) & 0x3F));
            return new Color(r / 255f, g / 255f, b / 255f, 0.28f);
        }
    }

    public bool TryGetAreaInfo(Vector2i indices, out TacticalMapAreaInfo info)
    {
        info = default;
        if (_currentAreaGridEntity == null)
            return false;

        string? areaId = null;
        string areaName = Loc.GetString("rmc-tacmap-alert-no-area");
        string? linkedLz = null;
        bool hasArea = false;
        bool cas = false;
        bool mortarFire = false;
        bool mortarPlacement = false;
        bool lasing = false;
        bool medevac = false;
        bool paradropping = false;
        bool orbitalBombard = false;
        bool supplyDrop = false;
        bool fulton = false;
        bool landingZone = false;
        string? areaLabel = null;

        if (!_entitySystemManager.TryGetEntitySystem(out SharedAreaLookupSystem? areaLookupSystem))
            return false;

        if (areaLookupSystem.TryGetArea(_currentAreaGridEntity.Value, indices, out var area, out var areaPrototype))
        {
            hasArea = true;
            areaId = areaPrototype.ID;
            areaName = areaPrototype.Name;

            var areaComp = area.Value.Comp;
            cas = areaComp.CAS;
            mortarFire = areaComp.MortarFire;
            mortarPlacement = areaComp.MortarPlacement;
            lasing = areaComp.Lasing;
            medevac = areaComp.Medevac;
            paradropping = areaComp.Paradropping;
            orbitalBombard = areaComp.OB;
            supplyDrop = areaComp.SupplyDrop;
            fulton = areaComp.Fulton;
            landingZone = areaComp.LandingZone;
            linkedLz = areaComp.LinkedLz;
        }

        areaLabel = _areaLabels.GetValueOrDefault(indices);
        string? tacticalLabel = null;
        if (TacticalLabels.TryGetValue(indices, out var tacticalData))
            tacticalLabel = tacticalData.Text;

        info = new TacticalMapAreaInfo(
            indices,
            areaName,
            areaId,
            areaLabel,
            tacticalLabel,
            hasArea,
            cas,
            mortarFire,
            mortarPlacement,
            lasing,
            medevac,
            paradropping,
            orbitalBombard,
            supplyDrop,
            fulton,
            landingZone,
            linkedLz);

        return true;
    }

    public void UpdateBlips(TacticalMapBlip[]? blips)
    {
        _blips = blips;
        _blipEntityIds = null;
    }

    public void UpdateBlips(TacticalMapBlip[]? blips, int[]? entityIds)
    {
        _blips = blips;
        _blipEntityIds = entityIds;
    }

    public void SetLocalPlayerEntityId(int? entityId)
    {
        _localPlayerEntityId = entityId;
    }

    public void UpdateTacticalLabels(Dictionary<Vector2i, TacticalMapLabelData> labels)
    {
        TacticalLabels.Clear();
        foreach ((Vector2i pos, TacticalMapLabelData data) in labels)
        {
            TacticalLabels[pos] = data;
        }
    }

    public void SetMortarOverlay(Vector2i center, int minRange, int maxRange, Color color)
    {
        _mortarOverlayCenter = center;
        _mortarOverlayMinRange = Math.Max(0, minRange);
        _mortarOverlayMaxRange = Math.Max(_mortarOverlayMinRange, maxRange);
        _mortarOverlayColor = color;
        RebuildMortarOverlayTiles();
    }

    public void ClearMortarOverlay()
    {
        _mortarOverlayCenter = null;
        _mortarOverlayTiles.Clear();
    }

    public bool HasMortarOverlay => _mortarOverlayCenter != null;

    public bool IsMortarOverlayTile(Vector2i indices)
    {
        if (_mortarOverlayCenter == null)
            return false;

        if (_mortarOverlayCenter.Value == indices)
            return true;

        return _mortarOverlayTiles.Contains(indices);
    }

    private void RebuildMortarOverlayTiles()
    {
        _mortarOverlayTiles.Clear();

        if (_mortarOverlayCenter == null)
            return;

        if (!IsWithinMap(_mortarOverlayCenter.Value))
            return;

        int maxRange = _mortarOverlayMaxRange;
        int minRange = _mortarOverlayMinRange;
        if (maxRange <= 0)
            return;

        int maxRangeSquared = maxRange * maxRange;
        int minRangeSquared = minRange * minRange;
        Vector2i center = _mortarOverlayCenter.Value;

        for (int dx = -maxRange; dx <= maxRange; dx++)
        {
            for (int dy = -maxRange; dy <= maxRange; dy++)
            {
                int distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > maxRangeSquared || distanceSquared < minRangeSquared)
                    continue;

                Vector2i indices = new(center.X + dx, center.Y + dy);
                if (!IsWithinMap(indices))
                    continue;

                if (!IsMortarFireAllowed(indices))
                    continue;

                _mortarOverlayTiles.Add(indices);
            }
        }
    }

    private bool IsMortarFireAllowed(Vector2i indices)
    {
        if (TryGetAreaInfo(indices, out var info))
        {
            if (!info.MortarFire)
                return false;
        }

        return !IsRoofed(indices, roof => !roof.CanMortarFire);
    }

    private bool IsOrbitalBombardAllowed(Vector2i indices)
    {
        if (TryGetAreaInfo(indices, out var info))
        {
            if (!info.OrbitalBombard)
                return false;
        }

        return !IsRoofed(indices, roof => !roof.CanOrbitalBombard);
    }

    private bool IsOrbitalBombardAreaAllowed(Vector2i indices)
    {
        if (TryGetAreaInfo(indices, out var info))
            return info.OrbitalBombard;

        return true;
    }

    private bool IsCasAreaAllowed(Vector2i indices)
    {
        if (TryGetAreaInfo(indices, out var info))
            return info.Cas;

        return true;
    }

    private bool IsRoofed(Vector2i indices, Predicate<RoofingEntityComponent> predicate)
    {
        if (_currentAreaGridEntity == null)
            return false;

        if (!_entityManager.TryGetComponent(_currentAreaGridEntity.Value, out MapGridComponent? grid))
            return false;

        var mapSystem = _entityManager.System<SharedMapSystem>();
        var roofs = _entityManager.EntityQueryEnumerator<RoofingEntityComponent, TransformComponent>();
        while (roofs.MoveNext(out _, out var roof, out var xform))
        {
            if (!predicate(roof))
                continue;

            if (xform.GridUid != _currentAreaGridEntity || xform.GridUid == null)
                continue;

            var roofTile = mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);
            float dx = indices.X - roofTile.X;
            float dy = indices.Y - roofTile.Y;
            if (dx * dx + dy * dy <= roof.Range * roof.Range)
                return true;
        }

        return false;
    }

    public void ShowTunnelInfo(Vector2i indices, string tunnelName, Vector2 screenPosition)
    {
        if (_tunnelInfoLabel == null)
            return;

        _tunnelInfoLabel.Text = tunnelName;
        _tunnelInfoLabel.Visible = true;

        LayoutContainer.SetPosition(_tunnelInfoLabel, screenPosition + new Vector2(-(_tunnelInfoLabel.DesiredSize.X / 2), -45));
    }

    public void HideTunnelInfo()
    {
        if (_tunnelInfoLabel != null)
            _tunnelInfoLabel.Visible = false;
    }
}
