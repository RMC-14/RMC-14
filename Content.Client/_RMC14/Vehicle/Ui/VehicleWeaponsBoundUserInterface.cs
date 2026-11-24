using System;
using System.Numerics;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleWeaponsBoundUserInterface : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private VehicleWeaponsMenu? _menu;

    public VehicleWeaponsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new VehicleWeaponsMenu();
        _menu.OnClose += Close;
        _menu.Title = string.Empty;

        _menu.OnSelect += mountedEntity => SendMessage(new VehicleWeaponsSelectMessage(mountedEntity));
        _menu.OnToggleStabilization += enabled => SendMessage(new VehicleWeaponsStabilizationMessage(enabled));
        _menu.OnToggleAutoTurret += enabled => SendMessage(new VehicleWeaponsAutoModeMessage(enabled));
        _menu.OnToggleSafety += () =>
        {
            if (EntMan.TryGetComponent(Owner, out VehicleHudSeatComponent? seat) &&
                EntMan.TryGetEntity(seat.Ui.Vehicle, out var vehicleUid))
            {
                EntMan.System<VehicleSafetySystem>().RequestToggle(vehicleUid.Value);
            }
        };
        _menu.OpenCenteredAt(new Vector2(0.7f, 0.05f));
        Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_menu != null)
            _menu.OnClose -= Close;

        _menu?.Dispose();
        _menu = null;
    }

    public void Refresh()
    {
        if (_menu is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out VehicleHudSeatComponent? seat))
            return;

        var weaponsState = seat.Ui;

        _menu.Update(
            weaponsState.Hardpoints,
            weaponsState.HasStabilization,
            weaponsState.CanToggleStabilization,
            weaponsState.StabilizationEnabled,
            weaponsState.CanToggleAuto,
            weaponsState.AutoEnabled);

        RefreshSafety(weaponsState.Vehicle);
    }

    private void RefreshSafety(NetEntity vehicle)
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetEntity(vehicle, out var vehicleUid) ||
            !EntMan.TryGetComponent(vehicleUid, out VehicleSafetyComponent? safety))
        {
            _menu.UpdateSafety(null, null, false, false);
            return;
        }

        var canToggle = EntMan.TryGetComponent(vehicleUid, out VehicleComponent? vehicleComp) &&
                        vehicleComp.Operator is { } driver &&
                        driver == _player.LocalEntity;

        _menu.UpdateSafety(safety.Icon, safety.Overlay, safety.Enabled, canToggle);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is VehicleWeaponsCooldownFeedbackMessage cooldown)
            _menu?.FlashCooldownFeedback(cooldown.RemainingSeconds);
    }
}
