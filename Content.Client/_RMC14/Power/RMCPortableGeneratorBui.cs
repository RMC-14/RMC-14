using Content.Client.Message;
using Content.Shared._RMC14.Power;
using Content.Shared.Materials;
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

        if (!EntMan.TryGetComponent(Owner, out RMCPortableGeneratorComponent? gen) ||
            !EntMan.TryGetComponent(Owner, out MaterialStorageComponent? storage))
            return;

        var anchored = EntMan.TryGetComponent(Owner, out TransformComponent? xform) && xform.Anchored;

        _window.StatusLabel.SetMarkupPermissive((!anchored, gen.On) switch
        {
            (true, _) => $"[color={RedColor.ToHex()}]Unanchored[/color]",
            (false, true) => $"[color={GreenColor.ToHex()}]Online[/color]",
            (false, false) => $"[color={RedColor.ToHex()}]Offline[/color]",
        });

        var fuelAmount = storage.Storage.GetValueOrDefault(gen.Material, 0);
        var fuelSheets = fuelAmount / gen.MaterialPerSheet;
        var fuelPercent = fuelAmount % (float)gen.MaterialPerSheet / gen.MaterialPerSheet * 100;

        _window.ToggleButton.Text = gen.On ? "Stop" : "Start";
        _window.ToggleButton.Disabled = !anchored || fuelAmount <= 0;


        _window.FuelLabel.SetMarkupPermissive(
            $"[color=#5B88B0]Fuel:[/color] [bold]{fuelSheets}[/bold] sheets of {gen.FuelName}");

        _window.FuelBar.MinValue = 0;
        _window.FuelBar.MaxValue = 100;
        _window.FuelBar.Value = fuelPercent;
        _window.FuelBarLabel.Text = $"{fuelPercent:F0}% of current sheet";

        _window.EjectButton.Disabled = fuelSheets < 1;

        var watts = gen.Watts * gen.PowerGenPercent / 100;
        _window.PowerOutputLabel.SetMarkupPermissive(
            $"[color=#5B88B0]Output:[/color] [bold]{watts} W[/bold] ({gen.PowerGenPercent}%)");

        _window.LowerPowerButton.Disabled = gen.PowerGenPercent <= gen.MinPowerPercent;
        _window.RaisePowerButton.Disabled = gen.PowerGenPercent >= gen.MaxPowerPercent;

        _window.HeatBar.MaxValue = gen.OverheatThreshold;
        _window.HeatBar.Value = Math.Min(gen.Heat, gen.OverheatThreshold);

        var heatStatus = gen.Heat switch
        {
            > 200 => $"[color={RedColor.ToHex()}]DANGER[/color]",
            >= 100 => $"[color={OrangeColor.ToHex()}]Caution[/color]",
            _ => $"[color={GreenColor.ToHex()}]Nominal[/color]",
        };

        _window.HeatStatusLabel.SetMarkupPermissive($"[color=#5B88B0]Heat:[/color] {heatStatus}");
    }

}
