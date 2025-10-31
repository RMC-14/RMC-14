using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Tools;
using Robust.Client.UserInterface.Controls;
using System.Numerics;

namespace Content.Client._RMC14.Labeler;

public sealed class RMCHandLabelerPillBottleColorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RMCPillBottleColorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new RMCPillBottleColorWindow();
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

    public RMCPillBottleColorWindow()
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

        var colors = Enum.GetValues<RMCPillBottleColors>();
        foreach (var color in colors)
        {
            var button = new Button
            {
                Text = color.ToString(),
                SetSize = new Vector2(90, 40)
            };

            button.OnPressed += _ => OnColorSelected?.Invoke(color);
            grid.AddChild(button);
        }

        scroll.AddChild(grid);
        AddChild(scroll);
    }
}
