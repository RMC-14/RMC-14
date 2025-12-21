using System.Collections.Generic;
using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Network;
using JetBrains.Annotations;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapUserBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;
    private static readonly ISawmill _logger = Logger.GetSawmill("tactical_map_settings");

    protected override TacticalMapWindow? Window { get; set; }
    private bool _refreshed;
    private string? _currentMapId;
    private IReadOnlyList<TacticalMapMapInfo> _availableMaps = new List<TacticalMapMapInfo>();
    private NetEntity _activeMap = NetEntity.Invalid;

    protected override void Open()
    {
        base.Open();

        Window = this.CreatePopOutableWindow<TacticalMapWindow>();
        Window.Wrapper.SetCanvasAccess(false);

        try
        {
            var settingsManager = IoCManager.Resolve<TacticalMapSettingsManager>();
            var settings = settingsManager.LoadSettings(_currentMapId);
            if (_currentMapId != null)
                Window.Wrapper.LoadMapSpecificSettings(settings, _currentMapId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load tactical map user settings for map '{_currentMapId}': {ex}");
        }

        Window.Wrapper.MapSelected += OnMapSelected;
        Window.Wrapper.LayerSelected += OnLayerSelected;
        ApplyMapState();
        TryUpdateTextureFromComponent();
        Refresh();

        Window.Wrapper.SetupUpdateButton(msg => SendPredictedMessage(msg));
        Window.Wrapper.Map.OnQueenEyeMove += position => SendPredictedMessage(new TacticalMapQueenEyeMoveMsg(position));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is TacticalMapBuiState tacticalState)
        {
            _availableMaps = tacticalState.Maps;
            _activeMap = tacticalState.ActiveMap;
            ApplyMapState();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Window?.Wrapper != null)
        {
            try
            {
                var settingsManager = IoCManager.Resolve<TacticalMapSettingsManager>();
                var currentSettings = Window.Wrapper.GetCurrentSettings();

                currentSettings.WindowSize = new Vector2(Window.SetSize.X, Window.SetSize.Y);
                currentSettings.WindowPosition = new Vector2(Window.Position.X, Window.Position.Y);

                settingsManager.SaveSettings(currentSettings, _currentMapId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save tactical map user settings during disposal for map '{_currentMapId}': {ex}");
            }
        }

        if (disposing && Window?.Wrapper != null)
        {
            Window.Wrapper.MapSelected -= OnMapSelected;
            Window.Wrapper.LayerSelected -= OnLayerSelected;
        }

        base.Dispose(disposing);
    }

    private void OnMapSelected(NetEntity map)
    {
        SendPredictedMessage(new TacticalMapSelectMapMsg(map));
    }

    private void OnLayerSelected(TacticalMapLayer? layer)
    {
        var layerId = layer.HasValue ? (int) layer.Value : TacticalMapSelectLayerMsg.AllLayersId;
        SendPredictedMessage(new TacticalMapSelectLayerMsg(layerId));
    }

    private void ApplyMapState()
    {
        if (Window == null)
            return;

        Window.Wrapper.UpdateMapList(_availableMaps, _activeMap);

        if (!TryGetActiveMapInfo(out var mapInfo))
            return;

        _currentMapId = mapInfo.MapId;
        Window.SetMapId(_currentMapId);
        Window.Wrapper.SetMapId(_currentMapId);

        if (EntMan.TryGetEntity(mapInfo.Map, out var mapEntity) &&
            EntMan.TryGetComponent(mapEntity.Value, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((mapEntity.Value, areaGrid));
        }

        UpdateLayerSelector();
    }

    private void UpdateLayerSelector()
    {
        if (Window == null)
            return;

        if (EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user))
            Window.Wrapper.UpdateLayerList(user.VisibleLayers, user.ActiveLayer);
        else
            Window.Wrapper.UpdateLayerList(new List<TacticalMapLayer>(), null);
    }

    private bool TryGetActiveMapInfo(out TacticalMapMapInfo mapInfo)
    {
        foreach (var map in _availableMaps)
        {
            if (map.Map == _activeMap)
            {
                mapInfo = map;
                return true;
            }
        }

        if (_availableMaps.Count > 0)
        {
            mapInfo = _availableMaps[0];
            return true;
        }

        mapInfo = default;
        return false;
    }

    private void TryUpdateTextureFromComponent()
    {
        if (Window == null || _availableMaps.Count > 0)
            return;

        if (EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user) &&
            user.Map != null &&
            EntMan.TryGetComponent(user.Map.Value, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((user.Map.Value, areaGrid));
        }
    }

    public void Refresh()
    {
        if (Window == null)
            return;

        UpdateLayerSelector();

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        Window.Wrapper.SetLineLimit(lineLimit);
        UpdateBlips();
        UpdateLabels();
        UpdateTimestamps();

        Window.Wrapper.Map.Lines.Clear();

        var lines = EntMan.GetComponentOrNull<TacticalMapLinesComponent>(Owner);
        if (lines != null)
            Window.Wrapper.Map.Lines.AddRange(lines.Lines);

        if (_refreshed)
            return;

        Window.Wrapper.Canvas.Lines.Clear();

        if (lines != null)
            Window.Wrapper.Canvas.Lines.AddRange(lines.Lines);

        var user = EntMan.GetComponentOrNull<TacticalMapUserComponent>(Owner);
        Window.Wrapper.SetCanvasAccess(user?.CanDraw ?? false);

        _refreshed = true;
    }

    private void UpdateBlips()
    {
        if (Window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user))
        {
            Window.Wrapper.UpdateBlips(null);
            return;
        }

        var blips = new TacticalMapBlip[user.Blips.Count];
        var entityIds = new int[user.Blips.Count];
        var i = 0;

        foreach (var (entityId, blip) in user.Blips)
        {
            blips[i] = blip;
            entityIds[i] = entityId;
            i++;
        }

        Window.Wrapper.UpdateBlips(blips, entityIds);

        int? localPlayerId = _player.LocalEntity != null
            ? (int?)EntMan.GetNetEntity(_player.LocalEntity.Value)
            : null;
        Window.Wrapper.Map.SetLocalPlayerEntityId(localPlayerId);
        Window.Wrapper.Canvas.SetLocalPlayerEntityId(localPlayerId);
    }

    private void UpdateLabels()
    {
        if (Window == null)
            return;

        var labels = EntMan.GetComponentOrNull<TacticalMapLabelsComponent>(Owner);
        if (labels != null)
        {
            Window.Wrapper.Map.UpdateTacticalLabels(labels.Labels);
            if (!_refreshed)
                Window.Wrapper.Canvas.UpdateTacticalLabels(labels.Labels);
        }
        else
        {
            Window.Wrapper.Map.UpdateTacticalLabels(new Dictionary<Vector2i, string>());
            if (!_refreshed)
                Window.Wrapper.Canvas.UpdateTacticalLabels(new Dictionary<Vector2i, string>());
        }
    }

    private void UpdateTimestamps()
    {
        if (Window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user))
            return;

        Window.Wrapper.LastUpdateAt = user.LastAnnounceAt;
        Window.Wrapper.NextUpdateAt = user.NextAnnounceAt;
    }
}
