using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

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

        TabContainer.SetTabTitle(Window.Wrapper.MapTab, "Map");
        TabContainer.SetTabVisible(Window.Wrapper.MapTab, true);

        if (EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user) &&
            EntMan.TryGetComponent(user.Map, out AreaGridComponent? areaGrid))
        {
            Window.Wrapper.UpdateTexture((user.Map.Value, areaGrid));
        }

        Refresh();

        Window.Wrapper.UpdateCanvasButton.OnPressed += _ => SendPredictedMessage(new TacticalMapUpdateCanvasMsg(Window.Wrapper.Canvas.Lines));
    }

    public void Refresh()
    {
        if (Window == null)
            return;

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        Window.Wrapper.SetLineLimit(lineLimit);
        UpdateBlips();

        var user = EntMan.GetComponentOrNull<TacticalMapUserComponent>(Owner);
        if (user != null)
        {
            Window.Wrapper.LastUpdateAt = user.LastAnnounceAt;
            Window.Wrapper.NextUpdateAt = user.NextAnnounceAt;
        }

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

        if (user?.CanDraw ?? false)
        {
            TabContainer.SetTabTitle(Window.Wrapper.CanvasTab, "Canvas");
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

        var blips = new TacticalMapBlip[user.MarineBlips.Count + user.XenoBlips.Count];
        var i = 0;

        foreach (var blip in user.MarineBlips.Values)
        {
            blips[i++] = blip;
        }

        foreach (var blip in user.XenoBlips.Values)
        {
            blips[i++] = blip;
        }

        Window.Wrapper.UpdateBlips(blips);
    }
}
