using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapUserBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;
    private static readonly ISawmill _logger = Logger.GetSawmill("tactical_map_settings");

    protected override TacticalMapWindow? Window { get; set; }
    private bool _refreshed;
    private string? _currentMapName;

    protected override void Open()
    {
        base.Open();

        EntityUid? mapEntity = null;

        if (EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user) && user.Map != null)
        {
            mapEntity = user.Map.Value;
        }

        Window = this.CreatePopOutableWindow<TacticalMapWindow>();

        if (mapEntity != null)
        {
            Window.SetMapEntity(_currentMapName);
        }

        TabContainer.SetTabTitle(Window.Wrapper.MapTab, Loc.GetString("ui-tactical-map-tab-map"));
        TabContainer.SetTabVisible(Window.Wrapper.MapTab, true);

        if (mapEntity != null)
        {
            Window.Wrapper.SetMapEntity(_currentMapName);
        }

        if (user?.Map != null &&
            EntMan.TryGetComponent(user.Map.Value, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((user.Map.Value, areaGrid));
        }

        try
        {
            var settingsManager = IoCManager.Resolve<TacticalMapSettingsManager>();
            var settings = settingsManager.LoadSettings(_currentMapName);
            if (_currentMapName != null)
            {
                Window.Wrapper.LoadMapSpecificSettings(settings, _currentMapName);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load tactical map user settings for map '{_currentMapName}': {ex}");
        }

        Refresh();

        Window.Wrapper.SetupUpdateButton(msg => SendPredictedMessage(msg));
        Window.Wrapper.Map.OnQueenEyeMove += position => SendPredictedMessage(new TacticalMapQueenEyeMoveMsg(position));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is TacticalMapBuiState tacticalState)
        {
            _currentMapName = tacticalState.MapName;
            Window?.SetMapEntity(_currentMapName);
            Window?.Wrapper.SetMapEntity(_currentMapName);
            Window?.Wrapper.UpdateSquadObjectivesFromState(tacticalState.SquadObjectives);
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

                settingsManager.SaveSettings(currentSettings, _currentMapName);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save tactical map user settings during disposal for map '{_currentMapName}': {ex}");
            }
        }

        base.Dispose(disposing);
    }

    public void Refresh()
    {
        if (Window == null)
            return;

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        Window.Wrapper.SetLineLimit(lineLimit);
        UpdateBlips();
        UpdateLabels();
        UpdateTimestamps();

        Window.Wrapper.Map.Lines.Clear();

        var lines = EntMan.GetComponentOrNull<TacticalMapLinesComponent>(Owner);
        if (lines != null)
        {
            Window.Wrapper.Map.Lines.AddRange(lines.MarineLines);
            Window.Wrapper.Map.Lines.AddRange(lines.XenoLines);
        }

        if (_refreshed)
            return;

        Window.Wrapper.Canvas.Lines.Clear();

        if (lines != null)
        {
            Window.Wrapper.Canvas.Lines.AddRange(lines.MarineLines);
            Window.Wrapper.Canvas.Lines.AddRange(lines.XenoLines);
        }

        var user = EntMan.GetComponentOrNull<TacticalMapUserComponent>(Owner);
        if (user?.CanDraw ?? false)
        {
            TabContainer.SetTabTitle(Window.Wrapper.CanvasTab, Loc.GetString("ui-tactical-map-tab-canvas"));
            TabContainer.SetTabVisible(Window.Wrapper.CanvasTab, true);
        }
        else
        {
            TabContainer.SetTabVisible(Window.Wrapper.CanvasTab, false);
        }

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

        var totalCount = user.MarineBlips.Count + user.XenoBlips.Count + user.XenoStructureBlips.Count;
        var blips = new TacticalMapBlip[totalCount];
        var entityIds = new int[totalCount];
        var i = 0;

        foreach (var (entityId, blip) in user.MarineBlips)
        {
            blips[i] = blip;
            entityIds[i] = entityId;
            i++;
        }

        foreach (var (entityId, blip) in user.XenoBlips)
        {
            blips[i] = blip;
            entityIds[i] = entityId;
            i++;
        }

        foreach (var (entityId, blip) in user.XenoStructureBlips)
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
            var allLabels = new Dictionary<Vector2i, string>();
            foreach (var label in labels.MarineLabels)
                allLabels[label.Key] = label.Value;
            foreach (var label in labels.XenoLabels)
                allLabels[label.Key] = label.Value;

            Window.Wrapper.Map.UpdateTacticalLabels(allLabels);
        }
        else
        {
            Window.Wrapper.Map.UpdateTacticalLabels(new Dictionary<Vector2i, string>());
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
