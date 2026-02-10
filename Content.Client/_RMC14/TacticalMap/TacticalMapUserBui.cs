using System.Collections.Generic;
using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Prototypes;
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
    private string? _lastCanvasLayerId;
    private readonly List<ProtoId<TacticalMapLayerPrototype>> _lastLayerOptions = new();
    private readonly List<ProtoId<TacticalMapLayerPrototype>> _lastVisibleLayers = new();
    private ProtoId<TacticalMapLayerPrototype>? _lastActiveLayer;
    private string? _currentMapId;
    private IReadOnlyList<TacticalMapMapInfo> _availableMaps = new List<TacticalMapMapInfo>();
    private NetEntity _activeMap = NetEntity.Invalid;
    private Dictionary<SquadObjectiveType, string> _objectives = new();

    protected override void Open()
    {
        base.Open();

        Window = this.CreatePopOutableWindow<TacticalMapWindow>();
        Window.Wrapper.SetCanvasAccess(false);
        Window.OnFinalClose += SaveSettings;

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
        Window.Wrapper.VisibleLayersChanged += OnVisibleLayersChanged;
        Window.Wrapper.CloseRequested += OnCloseRequested;
        ApplyMapState();
        TryUpdateTextureFromComponent();
        Refresh();
        Window.Wrapper.UpdateObjectives(_objectives);

        Window.Wrapper.SetupUpdateButton(msg => SendPredictedMessage(msg));
        Window.Wrapper.Map.OnQueenEyeMove += position => SendPredictedMessage(new TacticalMapQueenEyeMoveMsg(position));
        Window.Wrapper.Map.OnBlipEntityClicked = OnXenoWatchBlipClicked;
        Window.Wrapper.Canvas.OnBlipEntityClicked = OnXenoWatchBlipClicked;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is TacticalMapBuiState tacticalState)
        {
            _availableMaps = tacticalState.Maps;
            _activeMap = tacticalState.ActiveMap;
            _objectives = new Dictionary<SquadObjectiveType, string>(tacticalState.Objectives);
            ApplyMapState();
            Window?.Wrapper.UpdateObjectives(_objectives);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SaveSettings();

        if (disposing && Window?.Wrapper != null)
        {
            Window.Wrapper.MapSelected -= OnMapSelected;
            Window.Wrapper.LayerSelected -= OnLayerSelected;
            Window.Wrapper.VisibleLayersChanged -= OnVisibleLayersChanged;
            Window.Wrapper.CloseRequested -= OnCloseRequested;
            Window.OnFinalClose -= SaveSettings;
            Window.Wrapper.Map.OnBlipEntityClicked = null;
            Window.Wrapper.Canvas.OnBlipEntityClicked = null;
        }

        base.Dispose(disposing);
    }

    private void OnMapSelected(NetEntity map)
    {
        SendPredictedMessage(new TacticalMapSelectMapMsg(map));
    }

    private void OnCloseRequested()
    {
        SaveSettings();
        Close();
    }

    private void SaveSettings()
    {
        if (Window?.Wrapper == null)
            return;

        try
        {
            var settingsManager = IoCManager.Resolve<TacticalMapSettingsManager>();
            var currentSettings = Window.Wrapper.GetCurrentSettings();

            currentSettings.WindowSize = new Vector2(Window.SetSize.X, Window.SetSize.Y);
            currentSettings.WindowPosition = new Vector2(Window.Position.X, Window.Position.Y);

            var existingSettings = settingsManager.LoadSettings(_currentMapId);
            if (!existingSettings.NearlyEquals(currentSettings))
                settingsManager.SaveSettings(currentSettings, _currentMapId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save tactical map user settings for map '{_currentMapId}': {ex}");
        }
    }

    private void OnLayerSelected(ProtoId<TacticalMapLayerPrototype>? layer)
    {
        var layerId = layer?.Id;
        SendPredictedMessage(new TacticalMapSelectLayerMsg(layerId));
    }

    private void OnVisibleLayersChanged(IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> layers)
    {
        var ids = new List<string>(layers.Count);
        foreach (var layer in layers)
        {
            ids.Add(layer.Id);
        }

        SendPredictedMessage(new TacticalMapSetVisibleLayersMsg(ids));
    }

    private void OnXenoWatchBlipClicked(Vector2i indices, int? entityId)
    {
        if (entityId is null || entityId.Value <= 0)
            return;

        if (!EntMan.HasComponent<XenoComponent>(Owner))
            return;

        SendPredictedMessage(new TacticalMapXenoWatchBlipMsg(new NetEntity(entityId.Value)));
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
        {
            var options = user.LayerOptions.Count > 0 ? user.LayerOptions : user.VisibleLayers;
            var activeLayerId = user.ActiveLayer?.Id;
            if (_lastCanvasLayerId != activeLayerId)
            {
                _lastCanvasLayerId = activeLayerId;
                _refreshed = false;
                Window.Wrapper.Canvas.Lines.Clear();
                Window.Wrapper.Canvas.TacticalLabels.Clear();
            }

            var optionsChanged = !AreLayersEqual(options, _lastLayerOptions);
            var visibleChanged = !AreLayersEqual(user.VisibleLayers, _lastVisibleLayers);
            var activeChanged = _lastActiveLayer != user.ActiveLayer;

            if (optionsChanged || activeChanged)
                Window.Wrapper.UpdateDrawLayerList(options, user.ActiveLayer);

            if (optionsChanged || visibleChanged || activeChanged)
                Window.Wrapper.UpdateLayerVisibilityList(options, user.VisibleLayers);

            if (optionsChanged)
                ReplaceLayers(_lastLayerOptions, options);
            if (visibleChanged)
                ReplaceLayers(_lastVisibleLayers, user.VisibleLayers);
            if (activeChanged)
                _lastActiveLayer = user.ActiveLayer;
        }
        else
        {
            Window.Wrapper.UpdateDrawLayerList(new List<ProtoId<TacticalMapLayerPrototype>>(), null);
            Window.Wrapper.UpdateLayerVisibilityList(new List<ProtoId<TacticalMapLayerPrototype>>(), new List<ProtoId<TacticalMapLayerPrototype>>());
        }
    }

    private static bool AreLayersEqual(IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> first, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> second)
    {
        if (first.Count != second.Count)
            return false;

        for (var i = 0; i < first.Count; i++)
        {
            if (first[i] != second[i])
                return false;
        }

        return true;
    }

    private static void ReplaceLayers(List<ProtoId<TacticalMapLayerPrototype>> target, IReadOnlyList<ProtoId<TacticalMapLayerPrototype>> source)
    {
        target.Clear();
        for (var i = 0; i < source.Count; i++)
        {
            target.Add(source[i]);
        }
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

        var canApplyCanvas = Window.Wrapper.CanApplyCanvasSnapshot();
        if (canApplyCanvas)
        {
            Window.Wrapper.Canvas.Lines.Clear();

            var activeLines = EntMan.GetComponentOrNull<TacticalMapActiveLayerLinesComponent>(Owner);
            if (activeLines != null)
                Window.Wrapper.Canvas.Lines.AddRange(activeLines.Lines);
        }

        Window.Wrapper.UpdateCanvasBackground();
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
        }
        else
        {
            Window.Wrapper.Map.UpdateTacticalLabels(new Dictionary<Vector2i, TacticalMapLabelData>());
        }

        if (Window.Wrapper.CanApplyCanvasSnapshot())
        {
            var activeLabels = EntMan.GetComponentOrNull<TacticalMapActiveLayerLabelsComponent>(Owner);
            Window.Wrapper.Canvas.UpdateTacticalLabels(activeLabels?.Labels ?? new Dictionary<Vector2i, TacticalMapLabelData>());
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
        Window.Wrapper.SetBlipStaleState(!user.LiveUpdate, user.LastAnnounceAt);
    }
}
