using System;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class RMCVehiclePortGunBoundUserInterface : BoundUserInterface
{
    private RMCVehiclePortGunMenu? _menu;

    public RMCVehiclePortGunBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new RMCVehiclePortGunMenu();
        _menu.OnClose += Close;
        _menu.OnEject += OnEjectPressed;

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
        {
            _menu.SetGunName(metadata.EntityName);
        }

        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_menu != null)
        {
            _menu.OnClose -= Close;
            _menu.OnEject -= OnEjectPressed;
        }

        _menu?.Dispose();
        _menu = null;
    }

    private void OnEjectPressed()
    {
        SendMessage(new RMCVehiclePortGunEjectMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RMCVehiclePortGunUiState portState)
            return;

        _menu?.Update(portState);
    }
}
