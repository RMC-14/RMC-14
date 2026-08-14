using System;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleAmmoLoaderBoundUserInterface : BoundUserInterface, IRefreshableBui
{
    private VehicleAmmoLoaderMenu? _menu;

    public VehicleAmmoLoaderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new VehicleAmmoLoaderMenu();
        _menu.OnClose += Close;

        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metadata))
            _menu.Title = metadata.EntityName;

        _menu.OnSlotSelected += (slotPath, ammoSlot, action) =>
            SendMessage(new VehicleAmmoLoaderSelectMessage(slotPath, ammoSlot, action));
        _menu.OpenCentered();
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

        if (!EntMan.TryGetComponent(Owner, out VehicleAmmoLoaderComponent? loader))
            return;

        var ammoState = loader.Ui;
        _menu?.Update(ammoState.Hardpoints, ammoState.AmmoAmount, ammoState.AmmoMax, ammoState.AmmoPrototype);
    }
}
