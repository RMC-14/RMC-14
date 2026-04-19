using System;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class HardpointBoundUserInterface : BoundUserInterface
{
    private HardpointMenu? _menu;

    public HardpointBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new HardpointMenu();
        _menu.OnClose += Close;
        _menu.VehicleEntity = Owner;

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;

        _menu.OnRemove += slotId => SendMessage(new HardpointRemoveMessage(slotId));
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

        if (state is not HardpointBoundUserInterfaceState hardpointState)
            return;

        _menu?.Update(
            hardpointState.Hardpoints,
            hardpointState.FrameIntegrity,
            hardpointState.FrameMaxIntegrity,
            hardpointState.HasFrameIntegrity,
            hardpointState.Error);
    }
}
