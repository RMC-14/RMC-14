using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapComputerBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;

    protected override TacticalMapWindow? Window { get; set; }
    private bool _refreshed;
    private string? _currentMapName;

    protected override void Open()
    {
        base.Open();

        EntityUid? mapEntity = null;
        var computer = EntMan.GetComponentOrNull<TacticalMapComputerComponent>(Owner);
        if (computer?.Map != null)
        {
            mapEntity = computer.Map.Value;
        }

        Window = this.CreatePopOutableWindow<TacticalMapWindow>();

        if (_currentMapName != null)
        {
            Window.SetMapEntity(_currentMapName);
        }

        TabContainer.SetTabTitle(Window.Wrapper.MapTab, Loc.GetString("ui-tactical-map-tab-map"));
        TabContainer.SetTabVisible(Window.Wrapper.MapTab, true);

        if (computer != null &&
            _player.LocalEntity is { } player &&
            EntMan.System<SkillsSystem>().HasSkill(player, computer.Skill, computer.SkillLevel))
        {
            TabContainer.SetTabTitle(Window.Wrapper.CanvasTab, Loc.GetString("ui-tactical-map-tab-canvas"));
            TabContainer.SetTabVisible(Window.Wrapper.CanvasTab, true);
        }
        else
        {
            TabContainer.SetTabVisible(Window.Wrapper.CanvasTab, false);
        }

        if (computer != null &&
            EntMan.TryGetComponent(computer.Map, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((computer.Map.Value, areaGrid));
        }

        if (_currentMapName != null)
        {
            Window.Wrapper.SetMapEntity(_currentMapName);
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
            Logger.GetSawmill("tactical_map_settings").Error($"Failed to load tactical map settings for map '{_currentMapName}': {ex}");
        }

        Refresh();

        Window.Wrapper.UpdateCanvasButton.OnPressed += _ => SendPredictedMessage(new TacticalMapUpdateCanvasMsg(Window.Wrapper.Canvas.Lines, Window.Wrapper.Canvas.TacticalLabels));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is TacticalMapBuiState tacticalState)
        {
            _currentMapName = tacticalState.MapName;
            Window?.SetMapEntity(_currentMapName);
            Window?.Wrapper.SetMapEntity(_currentMapName);
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
                Logger.GetSawmill("tactical_map_settings").Error($"Failed to save tactical map settings during disposal for map '{_currentMapName}': {ex}");
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

        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            Window.Wrapper.LastUpdateAt = computer.LastAnnounceAt;
            Window.Wrapper.NextUpdateAt = computer.NextAnnounceAt;
        }

        Window.Wrapper.Map.Lines.Clear();

        var lines = EntMan.GetComponentOrNull<TacticalMapLinesComponent>(Owner);
        if (lines != null)
            Window.Wrapper.Map.Lines.AddRange(lines.MarineLines);

        if (_refreshed)
            return;

        if (lines != null)
            Window.Wrapper.Canvas.Lines.AddRange(lines.MarineLines);

        _refreshed = true;
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
        var entityIds = new int[computer.Blips.Count];
        var i = 0;

        foreach (var (entityId, blip) in computer.Blips)
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
            Window.Wrapper.Map.UpdateTacticalLabels(labels.MarineLabels);
            if (!_refreshed)
                Window.Wrapper.Canvas.UpdateTacticalLabels(labels.MarineLabels);
        }
        else
        {
            Window.Wrapper.Map.UpdateTacticalLabels(new Dictionary<Vector2i, string>());
            if (!_refreshed)
                Window.Wrapper.Canvas.UpdateTacticalLabels(new Dictionary<Vector2i, string>());
        }
    }
}
