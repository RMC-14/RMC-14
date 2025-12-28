using System;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed partial class RMCVehiclePortGunMenu : FancyWindow
{
    public event Action? OnEject;
    private readonly Label _gunTitle;
    private readonly Label _ammoLabel;
    private readonly Label _magazineLabel;
    private readonly Button _ejectButton;

    public RMCVehiclePortGunMenu()
    {
        RobustXamlLoader.Load(this);
        _gunTitle = FindControl<Label>("GunTitle");
        _ammoLabel = FindControl<Label>("AmmoLabel");
        _magazineLabel = FindControl<Label>("MagazineLabel");
        _ejectButton = FindControl<Button>("EjectButton");

        SetGunName(Loc.GetString("rmc-vehicle-portgun-ui-title"));
        _ejectButton.Text = Loc.GetString("rmc-vehicle-portgun-ui-eject");
        _ejectButton.OnPressed += _ => OnEject?.Invoke();
    }

    public void SetGunName(string name)
    {
        Title = name;
        _gunTitle.Text = name;
    }

    public void Update(RMCVehiclePortGunUiState state)
    {
        if (state.AmmoCapacity > 0)
        {
            _ammoLabel.Text = Loc.GetString("rmc-vehicle-portgun-ui-ammo",
                ("current", state.AmmoCount),
                ("max", state.AmmoCapacity));
        }
        else
        {
            _ammoLabel.Text = Loc.GetString("rmc-vehicle-portgun-ui-ammo-none");
        }

        _magazineLabel.Text = state.HasMagazine
            ? Loc.GetString("rmc-vehicle-portgun-ui-magazine-loaded")
            : Loc.GetString("rmc-vehicle-portgun-ui-magazine-empty");

        _ejectButton.Disabled = !state.HasMagazine;
    }
}
