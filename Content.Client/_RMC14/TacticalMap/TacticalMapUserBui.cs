using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapUserBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private TacticalMapWindow? _window;
    private bool _refreshed;

    public TacticalMapUserBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        _window = this.CreateWindow<TacticalMapWindow>();

        if (!EntMan.TryGetComponent(_player.LocalEntity, out TransformComponent? xform) ||
            !EntMan.TryGetComponent(xform.MapUid, out AreaGridComponent? areaGrid))
        {
            return;
        }

        TabContainer.SetTabTitle(_window.MapTab, "Map");
        TabContainer.SetTabVisible(_window.MapTab, true);

        _window.UpdateTexture((xform.MapUid.Value, areaGrid));
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        _window.SetLineLimit(lineLimit);
        UpdateBlips();

        _window.Map.Lines.Clear();
        _window.Canvas.Lines.Clear();

        if (_refreshed)
            return;

        var user = EntMan.GetComponentOrNull<TacticalMapUserComponent>(Owner);
        if (user != null)
        {
            _window.Map.Lines.AddRange(user.MarineLines);
            _window.Map.Lines.AddRange(user.XenoLines);
        }

        if (user?.CanDraw ?? false)
        {
            _window.Canvas.Lines.AddRange(user.MarineLines);
            _window.Canvas.Lines.AddRange(user.XenoLines);
            TabContainer.SetTabTitle(_window.CanvasTab, "Canvas");
            TabContainer.SetTabVisible(_window.CanvasTab, true);
        }
        else
        {
            TabContainer.SetTabVisible(_window.CanvasTab, false);
        }

        _refreshed = true;
    }

    private void UpdateBlips()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out TacticalMapUserComponent? user))
        {
            _window.UpdateBlips(null);
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

        _window.UpdateBlips(blips);
    }
}
