using Content.Shared._RMC14.Vehicle.Supply;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.Vehicle.Supply;

public sealed class RMCVehicleHardpointVendorBui : BoundUserInterface
{
    private RMCVehicleHardpointVendorWindow? _window;
    private string? _selectedVehicleId;
    private string? _selectedHardpointId;

    public RMCVehicleHardpointVendorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCVehicleHardpointVendorWindow>();

        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metaData))
            _window.Title = metaData.EntityName;

        _window.VehicleList.OnItemSelected += OnVehicleSelected;
        _window.HardpointList.OnItemSelected += OnHardpointSelected;
        _window.PrintButton.OnPressed += _ => PrintSelected();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RMCVehicleHardpointVendorBuiState uiState || _window == null)
            return;

        UpdateVehicleList(uiState);
        UpdateHardpointList(uiState);
    }

    private void UpdateVehicleList(RMCVehicleHardpointVendorBuiState state)
    {
        if (_window == null)
            return;

        _window.VehicleList.Clear();
        foreach (var entry in state.Vehicles)
        {
            var item = new ItemList.Item(_window.VehicleList)
            {
                Text = entry.Name,
                Metadata = entry.Id
            };
            _window.VehicleList.Add(item);
        }

        _selectedVehicleId = state.SelectedVehicle;
    }

    private void UpdateHardpointList(RMCVehicleHardpointVendorBuiState state)
    {
        if (_window == null)
            return;

        _window.HardpointList.Clear();
        foreach (var entry in state.Hardpoints)
        {
            var item = new ItemList.Item(_window.HardpointList)
            {
                Text = entry.Name,
                Metadata = entry.Id
            };
            _window.HardpointList.Add(item);
        }

        if (_selectedHardpointId != null && !HasHardpoint(_selectedHardpointId))
            _selectedHardpointId = null;

        _window.PrintButton.Disabled = _selectedHardpointId == null;
    }

    private bool HasHardpoint(string hardpointId)
    {
        if (_window == null)
            return false;

        foreach (var item in _window.HardpointList)
        {
            if (item.Metadata is string id && id == hardpointId)
                return true;
        }

        return false;
    }

    private void OnVehicleSelected(ItemList.ItemListSelectedEventArgs args)
    {
        if (args.ItemList[args.ItemIndex].Metadata is not string id)
            return;

        _selectedVehicleId = id;
        _selectedHardpointId = null;
        SendMessage(new RMCVehicleHardpointVendorSelectMsg(id));
    }

    private void OnHardpointSelected(ItemList.ItemListSelectedEventArgs args)
    {
        if (args.ItemList[args.ItemIndex].Metadata is not string id)
            return;

        _selectedHardpointId = id;
        if (_window != null)
            _window.PrintButton.Disabled = false;
    }

    private void PrintSelected()
    {
        if (_selectedHardpointId == null)
            return;

        SendMessage(new RMCVehicleHardpointVendorPrintMsg(_selectedHardpointId));
    }
}
