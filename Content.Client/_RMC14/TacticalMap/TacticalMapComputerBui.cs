using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapComputerBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private TacticalMapWindow? _window;
    private bool _refreshed;

    public TacticalMapComputerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
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

        TabContainer.SetTabTitle(_window.CanvasTab, "Canvas");
        TabContainer.SetTabVisible(_window.CanvasTab, true);

        _window.UpdateTexture((xform.MapUid.Value, areaGrid));
        Refresh();

        _window.UpdateCanvasButton.OnPressed += _ => SendPredictedMessage(new TacticalMapComputerUpdateCanvasMsg(_window.Canvas.Lines));
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        _window.SetLineLimit(lineLimit);
        UpdateBlips();

        if (_refreshed)
            return;

        _window.Canvas.Lines.Clear();
        _window.Map.Lines.Clear();

        if (EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            _window.Canvas.Lines.AddRange(computer.Lines);
            _window.Map.Lines.AddRange(computer.Lines);
        }

        _refreshed = true;
    }

    private void UpdateBlips()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out TacticalMapComputerComponent? computer))
        {
            _window.UpdateBlips(null);
            return;
        }

        var blips = new TacticalMapBlip[computer.Blips.Count];
        var i = 0;

        foreach (var blip in computer.Blips.Values)
        {
            blips[i++] = blip;
        }

        _window.UpdateBlips(blips);
    }
}
