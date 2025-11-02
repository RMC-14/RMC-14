using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Tools;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._RMC14.Labeler;

public sealed class RMCHandLabelerPillBottleColorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCPillBottleColorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window?.Close();

        var spriteSystem = EntMan.System<SpriteSystem>();
        _window = new RMCPillBottleColorWindow(spriteSystem);
        _window.OnColorSelected += OnColorSelected;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Close();
    }

    private void OnColorSelected(RMCPillBottleColors color)
    {
        if (!EntMan.TryGetComponent<RMCHandLabelerComponent>(Owner, out var comp) || comp.CurrentPillBottle == null)
            return;

        SendPredictedMessage(new RMCHandLabelerPillBottleColorMsg(EntMan.GetNetEntity(comp.CurrentPillBottle.Value), color));
        _window?.Close();
    }
}

public sealed class RMCPillBottleColorWindow : FancyWindow
{
    public event Action<RMCPillBottleColors>? OnColorSelected;

    public RMCPillBottleColorWindow(SpriteSystem spriteSystem)
    {
        Title = Loc.GetString("rmc-hand-labeler-pill-bottle-color");
        SetSize = new Vector2(300, 400);

        var scroll = new ScrollContainer
        {
            HScrollEnabled = false,
            VScrollEnabled = true
        };

        var grid = new GridContainer
        {
            Columns = 3,
            HSeparationOverride = 4,
            VSeparationOverride = 4
        };

        var pillCanisterRsi = new ResPath("_RMC14/Objects/Chemistry/pill_canister.rsi");
        var colors = Enum.GetValues<RMCPillBottleColors>();
        var colorCount = colors.Length - 1;

        for (var i = 0; i < colorCount; i++)
        {
            var state = spriteSystem.GetState(new SpriteSpecifier.Rsi(pillCanisterRsi, $"pill_canister{i}"));
            var button = new TextureButton
            {
                TextureNormal = state.Frame0,
                SetSize = new Vector2(90, 40),
                Scale = new Vector2(2, 2)
            };

            var color = colors[i];
            button.OnPressed += _ => OnColorSelected?.Invoke(color);
            grid.AddChild(button);
        }

        scroll.AddChild(grid);
        AddChild(scroll);
    }
}
