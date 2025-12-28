using System;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class RMCVehicleAmmoLoaderBoundUserInterface : BoundUserInterface
{
    private RMCVehicleAmmoLoaderMenu? _menu;

    public RMCVehicleAmmoLoaderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new RMCVehicleAmmoLoaderMenu();
        _menu.OnClose += Close;
        _menu.LoaderEntity = Owner;

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;

        _menu.OnLoad += slotId => SendMessage(new RMCVehicleAmmoLoaderSelectMessage(slotId));
        _menu.OpenCentered();
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

        if (state is not RMCVehicleAmmoLoaderUiState ammoState)
            return;

        _menu?.Update(ammoState.Hardpoints, ammoState.AmmoAmount, ammoState.AmmoMax);
    }
}
