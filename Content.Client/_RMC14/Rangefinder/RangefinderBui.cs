using Content.Client.Message;
using Content.Shared._RMC14.Rangefinder;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Rangefinder;

[UsedImplicitly]
public sealed class RangefinderBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RangefinderWindow? _window;

    protected override void Open()
    {
        _window = this.CreateWindow<RangefinderWindow>();
        _window.Header.SetMarkupPermissive(Loc.GetString("rmc-rangefinder-header"));
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (EntMan.TryGetComponent(Owner, out RangefinderComponent? rangefinder) &&
            rangefinder.LastTarget is { } target)
        {
            var msg = Loc.GetString("rmc-rangefinder-longitude", ("x", target.X));
            _window.Longitude.SetMarkupPermissive(msg);

            msg = Loc.GetString("rmc-rangefinder-latitude", ("y", target.Y));
            _window.Latitude.SetMarkupPermissive(msg);
        }
    }
}
