using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapUserBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    protected override TacticalMapWindow? Window { get; set; }
    private bool _refreshed;

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<TacticalMapWindow>();

        TabContainer.SetTabTitle(Window.Wrapper.MapTab, Loc.GetString("ui-tactical-map-tab-map"));
        TabContainer.SetTabVisible(Window.Wrapper.MapTab, true);

        if (EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user) &&
            user.Map != null &&
            EntMan.TryGetComponent(user.Map.Value, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((user.Map.Value, areaGrid));
        }

        Refresh();

        Window.Wrapper.SetupUpdateButton(msg => SendPredictedMessage(msg));
        Window.Wrapper.Map.OnQueenEyeMove += position => SendPredictedMessage(new TacticalMapQueenEyeMoveMsg(position));
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

        var blips = new TacticalMapBlip[user.MarineBlips.Count + user.XenoBlips.Count + user.XenoStructureBlips.Count];
        var i = 0;

        foreach (var blip in user.MarineBlips.Values)
        {
            blips[i++] = blip;
        }

        foreach (var blip in user.XenoBlips.Values)
        {
            blips[i++] = blip;
        }

        foreach (var blip in user.XenoStructureBlips.Values)
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
