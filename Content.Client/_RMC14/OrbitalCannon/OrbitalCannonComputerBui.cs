using System.Linq;
using System.Text;
using Content.Shared._RMC14.OrbitalCannon;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._RMC14.OrbitalCannon;

[UsedImplicitly]
public sealed class OrbitalCannonComputerBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ContainerSystem _container;
    private readonly OrbitalCannonSystem _orbitalCannon;

    private OrbitalCannonWindow? _window;

    public OrbitalCannonComputerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _orbitalCannon = EntMan.System<OrbitalCannonSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<OrbitalCannonWindow>();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out OrbitalCannonComputerComponent? computer))
            return;

        _window.WarheadStatusLabel.Text = Loc.GetString("rmc-ui-ob-warhead-none");
        if (computer.Warhead != null)
            _window.WarheadStatusLabel.Text = Loc.GetString("rmc-ui-ob-warhead-loaded", ("warhead", computer.Warhead));

        _window.FuelStatusLabel.Text = Loc.GetString("rmc-ui-ob-fuel-count", ("count", computer.Fuel));

        var requirements = new StringBuilder();
        foreach (var requirement in computer.FuelRequirements)
        {
            if (!_prototype.TryIndex(requirement.Warhead, out var warhead))
                continue;

            requirements.AppendLine(Loc.GetString("rmc-ui-ob-fuel-requirement",
                ("warhead", warhead.Name),
                ("fuel", requirement.Fuel)));
        }

        _window.FuelRequirementsLabel.Text = FormatRequirements(requirements.ToString().Trim());

        if (_orbitalCannon.TryGetClosestCannon(Owner, out var cannon) &&
            cannon.Comp.LinkedTray is { } trayId &&
            EntMan.TryGetComponent(trayId, out OrbitalCannonTrayComponent? tray))
        {
            _window.FuelProgressBar.Value = Math.Clamp(computer.Fuel, 0, tray.MaxFuel);
        }

        UpdateTrayControls(computer);
    }

    private string FormatRequirements(string requirements)
    {
        if (string.IsNullOrEmpty(requirements))
            return string.Empty;

        var formatted = new StringBuilder();
        var lines = requirements.Split('\n');

        foreach (var line in lines)
        {
            formatted.Append("• ");
            formatted.AppendLine(line);
        }

        return formatted.ToString();
    }

    private void UpdateTrayControls(OrbitalCannonComputerComponent computer)
    {
        if (_window == null)
            return;

        _window.TrayButtonOne.OnPressed -= LoadTray;
        _window.TrayButtonOne.OnPressed -= UnloadTray;
        _window.TrayButtonOne.OnPressed -= ChamberTray;
        _window.TrayButtonTwo.OnPressed -= LoadTray;
        _window.TrayButtonTwo.OnPressed -= UnloadTray;
        _window.TrayButtonTwo.OnPressed -= ChamberTray;

        switch (computer.Status)
        {
            case OrbitalCannonStatus.Unloaded:
                _window.TrayButtonOne.Text = Loc.GetString("rmc-ui-ob-load-tray");
                _window.TrayButtonOne.Visible = true;
                _window.TrayButtonOne.OnPressed += LoadTray;
                _window.TrayButtonTwo.Visible = false;
                _window.TrayButtonLabel.Visible = false;
                _window.TrayButtonOne.Disabled = computer.Warhead == null || computer.Fuel == 0;
                break;
            case OrbitalCannonStatus.Loaded:
                _window.TrayButtonOne.Text = Loc.GetString("rmc-ui-ob-unload-tray");
                _window.TrayButtonOne.Visible = true;
                _window.TrayButtonOne.OnPressed += UnloadTray;
                _window.TrayButtonTwo.Text = Loc.GetString("rmc-ui-ob-chamber-tray");
                _window.TrayButtonTwo.Visible = true;
                _window.TrayButtonTwo.OnPressed += ChamberTray;
                _window.TrayButtonLabel.Visible = false;
                break;
            case OrbitalCannonStatus.Chambered:
                _window.TrayButtonOne.Visible = false;
                _window.TrayButtonTwo.Visible = false;
                _window.TrayButtonLabel.Text = Loc.GetString("rmc-ui-ob-tray-chambered");
                _window.TrayButtonLabel.Visible = true;
                break;
        }
    }

    private void LoadTray(ButtonEventArgs args)
    {
        SendPredictedMessage(new OrbitalCannonComputerLoadBuiMsg());
    }

    private void UnloadTray(ButtonEventArgs args)
    {
        SendPredictedMessage(new OrbitalCannonComputerUnloadBuiMsg());
    }

    private void ChamberTray(ButtonEventArgs args)
    {
        SendPredictedMessage(new OrbitalCannonComputerChamberBuiMsg());
    }
}
