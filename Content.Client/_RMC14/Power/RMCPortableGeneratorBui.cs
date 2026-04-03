using Content.Client.Message;
using Content.Shared._RMC14.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Power;

[UsedImplicitly]
public sealed class RMCPortableGeneratorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color GreenColor = Color.FromHex("#5AC229");
    private static readonly Color RedColor = Color.FromHex("#CE3E31");
    private static readonly Color OrangeColor = Color.FromHex("#C99A29");

    [ViewVariables]
    private RMCPortableGeneratorWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCPortableGeneratorWindow>();

        _window.ToggleButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorToggleBuiMsg());
        _window.EjectButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorEjectFuelBuiMsg());
        _window.RaisePowerButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorRaisePowerBuiMsg());
        _window.LowerPowerButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorLowerPowerBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCPortableGeneratorComponent? gen))
            return;

        if (gen.On)
        {
            _window.StatusLabel.SetMarkupPermissive($"[color={GreenColor.ToHex()}][ Online ][/color]");
            _window.ToggleButton.Text = "Stop";
        }
        else
        {
            _window.StatusLabel.SetMarkupPermissive($"[color={RedColor.ToHex()}][ Offline ][/color]");
            _window.ToggleButton.Text = "Start";
        }

        var fuelPercent = gen.Sheets > 0 ? gen.SheetFraction * 100 : 0;
        _window.FuelLabel.SetMarkupPermissive(
            $"[color=#5B88B0]Fuel:[/color] [bold]{gen.Sheets}[/bold] sheets of {gen.FuelName} ({fuelPercent:F0}% of current sheet)");

        _window.FuelBar.MinValue = 0;
        _window.FuelBar.MaxValue = gen.MaxSheets;
        _window.FuelBar.Value = gen.Sheets;
        _window.FuelBarLabel.Text = $"{gen.Sheets} / {gen.MaxSheets}";

        _window.EjectButton.Disabled = gen.On;

        var watts = gen.Watts * gen.PowerGenPercent / 100;
        _window.PowerOutputLabel.SetMarkupPermissive(
            $"[color=#5B88B0]Output:[/color] [bold]{watts} W[/bold] ({gen.PowerGenPercent}%)");

        _window.LowerPowerButton.Disabled = gen.PowerGenPercent <= gen.MinPowerPercent;
        _window.RaisePowerButton.Disabled = gen.PowerGenPercent >= gen.MaxPowerPercent;

        _window.HeatBar.MinValue = 0;
        _window.HeatBar.MaxValue = gen.OverheatThreshold;
        _window.HeatBar.Value = Math.Min(gen.Heat, gen.OverheatThreshold);

        string heatStatus;
        if (gen.Heat > 200)
            heatStatus = $"[color={RedColor.ToHex()}]DANGER[/color]";
        else if (gen.Heat >= 100)
            heatStatus = $"[color={OrangeColor.ToHex()}]Caution[/color]";
        else
            heatStatus = $"[color={GreenColor.ToHex()}]Nominal[/color]";

        _window.HeatStatusLabel.SetMarkupPermissive($"[color=#5B88B0]Heat:[/color] {heatStatus}");
    }
}
