using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleSafetyUISystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly VehicleSafetySystem _vehicleSafety = default!;

    private VehicleSafetyIndicator? _indicator;

    public override void Shutdown()
    {
        _indicator?.Orphan();
        _indicator = null;
    }

    public override void FrameUpdate(float frameTime)
    {
        var show = _player.LocalEntity is { } user &&
                   TryComp(user, out VehicleOperatorComponent? op) &&
                   op.Vehicle is { } vehicle &&
                   _vehicleSafety.IsEnabled(vehicle);

        if (!show)
        {
            if (_indicator != null)
                _indicator.Visible = false;

            return;
        }

        if (_indicator == null)
        {
            _indicator = new VehicleSafetyIndicator();
            _ui.PopupRoot.AddChild(_indicator);
            LayoutContainer.SetAnchorAndMarginPreset(_indicator, LayoutContainer.LayoutPreset.CenterTop, margin: 8);
        }

        _indicator.Visible = true;
    }
}
