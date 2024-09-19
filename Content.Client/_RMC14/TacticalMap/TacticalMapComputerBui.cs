using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Shared._RMC14.TacticalMap.TacticalMapComponent;

namespace Content.Client._RMC14.TacticalMap;

[UsedImplicitly]
public sealed class TacticalMapComputerBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private TacticalMapWindow? _window;

    public TacticalMapComputerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        _window = this.CreateWindow<TacticalMapWindow>();

        if (!EntMan.TryGetComponent(_player.LocalEntity, out TransformComponent? xform) ||
            !EntMan.TryGetComponent(xform.MapUid, out AreaGridComponent? areaGrid) ||
            !EntMan.TryGetComponent(xform.MapUid, out TacticalMapComponent? tacticalMap))
        {
            return;
        }

        TabContainer.SetTabTitle(_window.MapTab, "Map");
        TabContainer.SetTabVisible(_window.MapTab, true);

        TabContainer.SetTabTitle(_window.CanvasTab, "Canvas");
        TabContainer.SetTabVisible(_window.CanvasTab, true);
        _window.Canvas.TextureScale = new Vector2(2, 2);

        var lineLimit = EntMan.System<TacticalMapSystem>().LineLimit;
        _window.SetLineLimit(lineLimit);
        _window.UpdateTexture((xform.MapUid.Value, areaGrid));
        UpdateBlips();

        _window.UpdateCanvasButton.OnPressed += _ => SendPredictedMessage(new TacticalMapComputerUpdateCanvasMsg(_window.Canvas.Lines));
        _window.Canvas.MouseFilter = Control.MouseFilterMode.Pass;
    }

    public void Refresh()
    {
        UpdateBlips();
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
