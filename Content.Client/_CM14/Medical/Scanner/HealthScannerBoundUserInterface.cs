using Content.Shared._CM14.Medical.Scanner;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Temperature;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CM14.Medical.Scanner;

[UsedImplicitly]
public sealed class HealthScannerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private HealthScannerWindow? _window;

    public HealthScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        if (State is HealthScannerBuiState state)
            UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is HealthScannerBuiState uiState)
            UpdateState(uiState);
    }

    private void UpdateState(HealthScannerBuiState uiState)
    {
        if (_window == null)
        {
            _window = new HealthScannerWindow { Title = "Health Scan" };
            _window.OnClose += Close;
        }

        if (_entities.GetEntity(uiState.Target) is not { Valid: true } target)
            return;

        _window.PatientLabel.Text = $"Patient: {Identity.Name(target, _entities, _player.LocalEntity)}";

        var thresholdsSystem = _entities.System<MobThresholdSystem>();
        if (_entities.TryGetComponent(target, out DamageableComponent? damageable))
        {
            var bruteMsg = new FormattedMessage();
            bruteMsg.AddText("Brute: ");
            bruteMsg.PushColor(Color.Red);
            bruteMsg.AddText(damageable.DamagePerGroup.GetValueOrDefault("Brute").Int().ToString());
            bruteMsg.Pop();
            _window.BruteLabel.SetMessage(bruteMsg);

            var burnMsg = new FormattedMessage();
            burnMsg.AddText("Burn: ");
            burnMsg.PushColor(Color.Orange);
            burnMsg.AddText(damageable.DamagePerGroup.GetValueOrDefault("Burn").Int().ToString());
            burnMsg.Pop();
            _window.BurnLabel.SetMessage(burnMsg);

            var toxinMsg = new FormattedMessage();
            toxinMsg.AddText("Toxin: ");
            toxinMsg.PushColor(Color.Green);
            toxinMsg.AddText(damageable.DamagePerGroup.GetValueOrDefault("Toxin").Int().ToString());
            toxinMsg.Pop();
            _window.ToxinLabel.SetMessage(toxinMsg);

            var oxygenMsg = new FormattedMessage();
            oxygenMsg.AddText("Oxygen: ");
            oxygenMsg.PushColor(Color.DeepSkyBlue);
            oxygenMsg.AddText(damageable.DamagePerGroup.GetValueOrDefault("Airloss").Int().ToString());
            oxygenMsg.Pop();
            _window.OxygenLabel.SetMessage(oxygenMsg);

            if (thresholdsSystem.TryGetIncapThreshold(target, out var threshold))
            {
                var damage = threshold.Value - damageable.TotalDamage;
                _window.HealthBar.MinValue = 0;
                _window.HealthBar.MaxValue = threshold.Value.Float();
                _window.HealthBar.Value = damage.Float() / threshold.Value.Float() * 100f;
                _window.HealthBarText.Text = $"{_window.HealthBar.Value:F}% healthy";
            }

            _window.ChemicalsContainer.DisposeAllChildren();

            var anyChemicals = false;
            if (uiState.Chemicals != null)
            {
                foreach (var reagent in uiState.Chemicals.Contents)
                {
                    if (!_prototype.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? prototype))
                        continue;

                    _window.ChemicalsContainer.AddChild(new Label { Text = $"{reagent.Quantity.Float():F1} {prototype.LocalizedName}"});
                    anyChemicals = true;
                }
            }

            _window.ChemicalContentsLabel.Visible = anyChemicals;
            _window.ChemicalContentsSeparator.Visible = anyChemicals;
            _window.ChemicalsContainer.Visible = anyChemicals;

            _window.BloodTypeLabel.Text = "Blood:";
            var bloodMsg = new FormattedMessage();
            bloodMsg.PushColor(Color.FromHex("#25B732"));
            bloodMsg.AddText($"{uiState.BloodPercentage:F2}%");
            bloodMsg.Pop();
            _window.BloodAmountLabel.SetMessage(bloodMsg);

            var temperatureMsg = new FormattedMessage();
            if (uiState.Temperature is { } temperatureKelvin)
            {
                var celsius = TemperatureHelpers.KelvinToCelsius(temperatureKelvin);
                var fahrenheit = TemperatureHelpers.KelvinToFahrenheit(temperatureKelvin);
                temperatureMsg.AddText($"{celsius:F1}ºC ({fahrenheit:F1}ºF)");
            }
            else
            {
                temperatureMsg.AddText("None");
            }

            _window.BodyTemperatureLabel.SetMessage(temperatureMsg);
        }

        if (!_window.IsOpen)
        {
            _window.OpenCentered();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
