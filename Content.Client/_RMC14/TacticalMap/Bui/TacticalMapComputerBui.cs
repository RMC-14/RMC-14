using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._RMC14.TacticalMap.UI;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapComputerBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;

    protected override TacticalMapWindow? Window { get; set; }
    private string? _lastCanvasLayerId;
    private readonly List<ProtoId<TacticalMapLayerPrototype>> _lastLayerOptions = new();
    private readonly List<ProtoId<TacticalMapLayerPrototype>> _lastVisibleLayers = new();
    private readonly List<ProtoId<TacticalMapLayerPrototype>> _lastDrawOptions = new();
    private ProtoId<TacticalMapLayerPrototype>? _lastActiveLayer;
    private string? _currentMapId;
    private IReadOnlyList<TacticalMapMapInfo> _availableMaps = new List<TacticalMapMapInfo>();
    private NetEntity _activeMap = NetEntity.Invalid;
    private Dictionary<SquadObjectiveType, string> _objectives = new();

    protected override void Open()
    {
        base.Open();

        var computer = EntMan.GetComponentOrNull<TacticalMapComputerComponent>(Owner);
        Window = this.CreatePopOutableWindow<TacticalMapWindow>();
        Window.OnFinalClose += SaveSettings;
        var canDraw = computer != null &&
            (EntMan.HasComponent<MarineCommunicationsComputerComponent>(Owner) ||
             EntMan.HasComponent<OverwatchConsoleComponent>(Owner)) &&
            _player.LocalEntity is { } player &&
            EntMan.System<SkillsSystem>().HasSkill(player, computer.Skill, computer.SkillLevel);
        Window.Wrapper.SetCanvasAccess(canDraw);

        try
        {
            var settingsManager = IoCManager.Resolve<TacticalMapSettingsManager>();
            var settings = settingsManager.LoadSettings(_currentMapId);
            if (_currentMapId != null)
                Window.Wrapper.LoadMapSpecificSettings(settings, _currentMapId);
        }
        catch (Exception ex)
        {
            Logger.GetSawmill("tactical_map_settings").Error($"Failed to load tactical map settings for map '{_currentMapId}': {ex}");
        }

        Window.Wrapper.MapSelected += OnMapSelected;
        Window.Wrapper.LayerSelected += OnLayerSelected;
        Window.Wrapper.VisibleLayersChanged += OnVisibleLayersChanged;
        Window.Wrapper.CloseRequested += OnCloseRequested;
        TryUpdateTextureFromComponent();
        Refresh();
        Window.Wrapper.Map.OnQueenEyeMove += position => SendPredictedMessage(new TacticalMapQueenEyeMoveMsg(position));

        if (EntMan.HasComponent<OverwatchConsoleComponent>(Owner))
        {
            Window.Wrapper.Map.OnBlipEntityClicked = OnOverwatchBlipClicked;
            Window.Wrapper.Canvas.OnBlipEntityClicked = OnOverwatchBlipClicked;
        }

        Window.Wrapper.UpdateCanvasButton.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new TacticalMapUpdateCanvasMsg(Window.Wrapper.Canvas.Lines, Window.Wrapper.Canvas.TacticalLabels));
            Window.Wrapper.NotifyCanvasUpdated();
        };
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
            Logger.GetSawmill("tactical_map_settings").Error($"Failed to save tactical map settings for map '{_currentMapId}': {ex}");
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

    private void OnOverwatchBlipClicked(Vector2i indices, NetEntity? entityId)
    {
        if (entityId is null || entityId.Value == NetEntity.Invalid)
            return;

        SendPredictedMessage(new TacticalMapOverwatchBlipMsg(entityId.Value));
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

        // cache layer lists
        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            var options = computer.LayerOptions.Count > 0 ? computer.LayerOptions : computer.VisibleLayers;
            var drawOptions = options;
            if (EntMan.HasComponent<OverwatchConsoleComponent>(Owner) &&
                !EntMan.HasComponent<MarineCommunicationsComputerComponent>(Owner))
            {
                // overwatch can see more layers than it is allowed to draw on
                drawOptions = computer.ActiveLayer != null
                    ? new List<ProtoId<TacticalMapLayerPrototype>> { computer.ActiveLayer.Value }
                    : new List<ProtoId<TacticalMapLayerPrototype>>();
            }

            var activeLayerId = computer.ActiveLayer?.Id;
            if (_lastCanvasLayerId != activeLayerId)
            {
                _lastCanvasLayerId = activeLayerId;
                Window.Wrapper.Canvas.Lines.Clear();
                Window.Wrapper.Canvas.TacticalLabels.Clear();
            }

            var optionsChanged = !AreLayersEqual(options, _lastLayerOptions);
            var visibleChanged = !AreLayersEqual(computer.VisibleLayers, _lastVisibleLayers);
            var drawOptionsChanged = !AreLayersEqual(drawOptions, _lastDrawOptions);
            var activeChanged = _lastActiveLayer != computer.ActiveLayer;

            if (drawOptionsChanged || activeChanged)
                Window.Wrapper.UpdateDrawLayerList(drawOptions, computer.ActiveLayer);

            if (optionsChanged || visibleChanged || activeChanged)
                Window.Wrapper.UpdateLayerVisibilityList(options, computer.VisibleLayers);

            if (optionsChanged)
                ReplaceLayers(_lastLayerOptions, options);
            if (visibleChanged)
                ReplaceLayers(_lastVisibleLayers, computer.VisibleLayers);
            if (drawOptionsChanged)
                ReplaceLayers(_lastDrawOptions, drawOptions);
            if (activeChanged)
                _lastActiveLayer = computer.ActiveLayer;
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

        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer) &&
            computer.Map != null &&
            EntMan.TryGetComponent(computer.Map.Value, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((computer.Map.Value, areaGrid));
        }
    }

    private void RefreshMapState()
    {
        if (Window == null)
            return;

        var maps = BuildAvailableMaps();
        var mapsChanged = !maps.SequenceEqual(_availableMaps);
        _availableMaps = maps;

        NetEntity activeMap;
        Dictionary<SquadObjectiveType, string> objectives;
        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            activeMap = computer.Map != null ? EntMan.GetNetEntity(computer.Map.Value) : NetEntity.Invalid;
            objectives = computer.Objectives;
        }
        else
        {
            activeMap = NetEntity.Invalid;
            objectives = new Dictionary<SquadObjectiveType, string>();
        }

        var activeMapChanged = activeMap != _activeMap;
        _activeMap = activeMap;

        if (mapsChanged || activeMapChanged)
            ApplyMapState();

        if (!DictionariesEqual(_objectives, objectives))
        {
            _objectives = new Dictionary<SquadObjectiveType, string>(objectives);
            Window.Wrapper.UpdateObjectives(_objectives);
        }
    }

    private static bool DictionariesEqual(Dictionary<SquadObjectiveType, string> first, Dictionary<SquadObjectiveType, string> second)
    {
        if (first.Count != second.Count)
            return false;

        foreach (var (key, value) in first)
        {
            if (!second.TryGetValue(key, out var otherValue) || value != otherValue)
                return false;
        }

        return true;
    }

    private List<TacticalMapMapInfo> BuildAvailableMaps()
    {
        var maps = new List<TacticalMapMapInfo>();
        var query = EntMan.EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var uid, out var map))
        {
            maps.Add(new TacticalMapMapInfo(EntMan.GetNetEntity(uid), map.MapId, map.DisplayName));
        }

        maps.Sort(static (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return maps;
    }

    public void Refresh()
    {
        if (Window == null)
            return;

        RefreshMapState();

        // the map shows visible layers, the canvas shows only the active layer
        UpdateLayerSelector();

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        Window.Wrapper.SetLineLimit(lineLimit);
        UpdateBlips();
        UpdateLabels();

        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            // computers receive live blips, so stale fading stays disabled
            Window.Wrapper.LastUpdateAt = computer.LastAnnounceAt;
            Window.Wrapper.NextUpdateAt = computer.NextAnnounceAt;
            Window.Wrapper.SetBlipStaleState(false, computer.LastAnnounceAt);
        }

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
    }

    private void UpdateBlips()
    {
        if (Window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            Window.Wrapper.UpdateBlips(null);
            return;
        }

        var blips = new TacticalMapBlip[computer.Blips.Count];
        var entities = new NetEntity[computer.Blips.Count];
        var i = 0;

        foreach (var (entity, blip) in computer.Blips)
        {
            blips[i] = blip;
            entities[i] = entity;
            i++;
        }

        Window.Wrapper.UpdateBlips(blips, entities);

        NetEntity? localPlayer = _player.LocalEntity != null
            ? EntMan.GetNetEntity(_player.LocalEntity.Value)
            : null;
        Window.Wrapper.Map.SetLocalPlayerEntity(localPlayer);
        Window.Wrapper.Canvas.SetLocalPlayerEntity(localPlayer);
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
}
