using System;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class RMCVehicleWeaponsBoundUserInterface : BoundUserInterface
{
    private RMCVehicleWeaponsMenu? _menu;

    public RMCVehicleWeaponsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new RMCVehicleWeaponsMenu();
        _menu.OnClose += Close;

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;

        _menu.OnSelect += slotId => SendMessage(new RMCVehicleWeaponsSelectMessage(slotId));
        _menu.OnToggleStabilization += enabled => SendMessage(new RMCVehicleWeaponsStabilizationMessage(enabled));
        _menu.OnToggleAutoTurret += enabled => SendMessage(new RMCVehicleWeaponsAutoModeMessage(enabled));
        _menu.OpenCenteredAt(new Vector2(0.1f, 0.9f));
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

        if (state is not RMCVehicleWeaponsUiState weaponsState)
        {
            return;
        }

        _menu?.Update(
            weaponsState.Hardpoints,
            weaponsState.CanToggleStabilization,
            weaponsState.StabilizationEnabled,
            weaponsState.CanToggleAuto,
            weaponsState.AutoEnabled);
    }
}
