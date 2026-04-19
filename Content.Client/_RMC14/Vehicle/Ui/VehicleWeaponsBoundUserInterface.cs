using System;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleWeaponsBoundUserInterface : BoundUserInterface
{
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
        _menu.OpenCenteredAt(new Vector2(0.7f, 0.05f));
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

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not VehicleWeaponsUiState weaponsState)
        {
            return;
        }

        _menu?.Update(
            weaponsState.Vehicle,
            weaponsState.Hardpoints,
            weaponsState.CanToggleStabilization,
            weaponsState.StabilizationEnabled,
            weaponsState.CanToggleAuto,
            weaponsState.AutoEnabled);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is VehicleWeaponsCooldownFeedbackMessage cooldown)
            _menu?.FlashCooldownFeedback(cooldown.RemainingSeconds);
    }
}
