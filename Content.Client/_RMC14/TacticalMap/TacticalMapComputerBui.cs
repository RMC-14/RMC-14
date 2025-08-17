using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapComputerBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;

    protected override TacticalMapWindow? Window { get; set; }
    private bool _refreshed;

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<TacticalMapWindow>();

        TabContainer.SetTabTitle(Window.Wrapper.MapTab, Loc.GetString("ui-tactical-map-tab-map"));
        TabContainer.SetTabVisible(Window.Wrapper.MapTab, true);

        var computer = EntMan.GetComponentOrNull<TacticalMapComputerComponent>(Owner);
        var skills = EntMan.System<SkillsSystem>();
        if (computer != null &&
            _player.LocalEntity is { } player &&
            skills.HasSkill(player, computer.Skill, computer.SkillLevel))
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

        Refresh();

        Window.Wrapper.UpdateCanvasButton.OnPressed += _ => SendPredictedMessage(new TacticalMapUpdateCanvasMsg(Window.Wrapper.Canvas.Lines, Window.Wrapper.Canvas.TacticalLabels));
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
        var i = 0;

        foreach (var blip in computer.Blips.Values)
        {
            blips[i++] = blip;
        }

        Window.Wrapper.UpdateBlips(blips);
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
