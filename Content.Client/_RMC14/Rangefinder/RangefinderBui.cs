using Content.Client.Message;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Rangefinder;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Rangefinder;

[UsedImplicitly]
public sealed class RangefinderBui : BoundUserInterface
{
    private RangefinderWindow? _window;

    private readonly AreaSystem _area;
    private readonly SharedTransformSystem _transform;

    public RangefinderBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _area = EntMan.System<AreaSystem>();
        _transform = EntMan.System<SharedTransformSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RangefinderWindow>();
        _window.Header.SetMarkupPermissive(Loc.GetString("rmc-rangefinder-header"));
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RangefinderComponent? rangefinder))
            return;

        if (rangefinder.LastTarget is { } target)
        {
            var msg = Loc.GetString("rmc-rangefinder-longitude", ("x", target.X));
            _window.Longitude.SetMarkupPermissive(msg);

            msg = Loc.GetString("rmc-rangefinder-latitude", ("y", target.Y));
            _window.Latitude.SetMarkupPermissive(msg);
        }

        _window.BottomContainer.DisposeAllChildren();

        if (rangefinder.LastCoords is { } mapCoords)
        {
            var coords = _transform.ToCoordinates(mapCoords);
            _window.BottomContainer.AddChild(AddRow("Supply Drop", _area.CanSupplyDrop(mapCoords)));
            _window.BottomContainer.AddChild(AddRow("Mortar", _area.CanMortarFire(coords)));
            _window.BottomContainer.AddChild(AddRow("Close Air Support", _area.CanCAS(coords)));
            _window.BottomContainer.AddChild(AddRow("Orbital Bombardment", _area.CanOrbitalBombard(coords, out _)));
        }
    }

    private BoxContainer AddRow(string text, bool allowed)
    {
        var container = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
        var label = new RichTextLabel();
        label.SetMarkup($"{(allowed ? "[color=green]✓[/color]" : "[color=red]X[/color]")} {text}");
        container.AddChild(label);
        return container;
    }
}
