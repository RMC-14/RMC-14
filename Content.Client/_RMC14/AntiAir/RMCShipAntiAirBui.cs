using Content.Client.Message;
using Content.Shared._RMC14.AntiAir;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.AntiAir;

[UsedImplicitly]
public sealed class RMCShipAntiAirBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey), IRefreshableBui
{
    private RMCShipAntiAirWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCShipAntiAirWindow>();
        _window.ClearButton.OnPressed += _ => SendPredictedMessage(new RMCShipAntiAirClearZoneBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCShipAntiAirComponent? antiAir))
            return;

        Refresh(antiAir);
    }

    private void Refresh(RMCShipAntiAirComponent antiAir)
    {
        if (_window == null)
            return;

        var protectedZone = antiAir.ProtectedZone ?? Loc.GetString("rmc-anti-air-zone-none");

        var status = antiAir.Disabled
            ? Loc.GetString("rmc-anti-air-status-disabled")
            : Loc.GetString("rmc-anti-air-status-operational");

        _window.Status.SetMarkupPermissive(Loc.GetString("rmc-anti-air-status",
            ("status", status),
            ("zone", protectedZone)));

        _window.ClearButton.Text = Loc.GetString("rmc-anti-air-clear");
        _window.ClearButton.Disabled = antiAir.Disabled || antiAir.ProtectedZone == null;

        _window.Zones.DisposeAllChildren();
        foreach (var zone in antiAir.Zones)
        {
            var selected = zone.Zone == antiAir.ProtectedZone;
            var button = new Button
            {
                Text = selected
                    ? Loc.GetString("rmc-anti-air-zone-selected", ("zone", zone.Zone))
                    : zone.Zone,
                Disabled = antiAir.Disabled || selected,
                StyleClasses = { "OpenBoth" },
                Margin = new Thickness(0, 0, 0, 4),
            };

            var defenseZone = zone.Zone;
            button.OnPressed += _ => SendPredictedMessage(new RMCShipAntiAirSetZoneBuiMsg(defenseZone));
            _window.Zones.AddChild(button);
        }
    }
}
