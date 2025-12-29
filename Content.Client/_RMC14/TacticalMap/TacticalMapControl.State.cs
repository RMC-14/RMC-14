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
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._RMC14.TacticalMap;

public sealed partial class TacticalMapControl
{
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
        Texture = Texture.LoadFromImage(image);
        _overlayLinkedLzTexture = hasLinkedLzOverlay && linkedLzOverlay != null
            ? Texture.LoadFromImage(linkedLzOverlay)
            : null;
        _overlayIconTextures.Clear();
        foreach (var (state, iconOverlay) in iconOverlays)
        {
            _overlayIconTextures[state] = Texture.LoadFromImage(iconOverlay);
        }
        _areaLabels.Clear();
        foreach ((Vector2i position, string label) in grid.Comp.Labels)
        {
            if (position.X < boundsMin.X || position.X > boundsMax.X ||
                position.Y < boundsMin.Y || position.Y > boundsMax.Y)
            {
                continue;
            }

            _areaLabels[position] = label;
        }

        ApplyViewSettings();
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
