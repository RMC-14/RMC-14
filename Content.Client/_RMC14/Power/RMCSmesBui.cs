using Content.Shared._RMC14.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Power;

[UsedImplicitly]
public sealed class RMCSmesBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color GreenColor = Color.FromHex("#5AC229");
    private static readonly Color OrangeColor = Color.FromHex("#C99A29");
    private static readonly Color RedColor = Color.FromHex("#CE3E31");
    private static readonly Color InactiveColor = Color.FromHex("#9A9A9A");

    [ViewVariables]
    private RMCSmesWindow? _window;

    private int _maxInputKilowatts = 200;
    private int _maxOutputKilowatts = 200;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCSmesWindow>();
        _window.InputButton.OnPressed += args => ToggleInput(args.Button.Pressed);
        _window.OutputButton.OnPressed += args => ToggleOutput(args.Button.Pressed);
        _window.InputMinButton.OnPressed += _ => _window.InputLimit.Value = 0;
        _window.InputMaxButton.OnPressed += _ => _window.InputLimit.Value = _maxInputKilowatts;
        _window.OutputMinButton.OnPressed += _ => _window.OutputLimit.Value = 0;
        _window.OutputMaxButton.OnPressed += _ => _window.OutputLimit.Value = _maxOutputKilowatts;
        _window.InputLimit.IsValid = value => value >= 0 && value <= _maxInputKilowatts;
        _window.OutputLimit.IsValid = value => value >= 0 && value <= _maxOutputKilowatts;
        _window.InputLimit.AddLeftButton(-10, "-10");
        _window.InputLimit.AddRightButton(10, "+10");
        _window.OutputLimit.AddLeftButton(-10, "-10");
        _window.OutputLimit.AddRightButton(10, "+10");
        _window.InputLimit.ValueChanged += args =>
            SendPredictedMessage(new RMCSmesSetInputLimitBuiMsg(args.Value * 1000));
        _window.OutputLimit.ValueChanged += args =>
            SendPredictedMessage(new RMCSmesSetOutputLimitBuiMsg(args.Value * 1000));
        Refresh();
    }

    public void Refresh(bool? inputEnabledOverride = null, bool? outputEnabledOverride = null)
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out RMCSmesComponent? smes) ||
            !EntMan.TryGetComponent(Owner, out RMCPowerStorageComponent? storage))
        {
            return;
        }

        _window.Title = Loc.GetString("rmc-smes-ui-title");
        _maxInputKilowatts = (int) MathF.Round(storage.MaxInput / 1000);
        _maxOutputKilowatts = (int) MathF.Round(storage.MaxOutput / 1000);
        var inputEnabled = inputEnabledOverride ?? storage.InputEnabled;
        var outputEnabled = outputEnabledOverride ?? storage.OutputEnabled;
        var charge = Math.Clamp(smes.ChargePercentage, 0, 1);
        _window.ChargeLabel.Text = Loc.GetString("rmc-smes-ui-charge",
            ("charge", Number(smes.Charge / 1_000_000)),
            ("maxCharge", Number(smes.MaxCharge / 1_000_000)),
            ("percent", Number(charge * 100)));
        _window.ChargeLabel.FontColorOverride = Color.White;
        _window.ChargeBar.Value = charge;

        _window.InputButton.Text = Loc.GetString(inputEnabled
            ? "rmc-smes-ui-input-auto"
            : "rmc-smes-ui-off");
        _window.OutputButton.Text = Loc.GetString(outputEnabled
            ? "rmc-smes-ui-output-on"
            : "rmc-smes-ui-off");
        _window.InputButton.Pressed = inputEnabled;
        _window.OutputButton.Pressed = outputEnabled;
        _window.InputLimit.OverrideValue((int) MathF.Round(storage.InputLimit / 1000));
        _window.OutputLimit.OverrideValue((int) MathF.Round(storage.OutputLimit / 1000));
        _window.InputActual.Text = Power(storage.CurrentInput);
        _window.OutputActual.Text = Power(storage.CurrentOutput);

        SetFlowState(smes, storage);
        SetInputState(smes, storage, inputEnabled);
        SetOutputState(smes, storage, outputEnabled);

        _window.EmpPanel.Visible = smes.EmpDisabled;
        _window.InputButton.Disabled = smes.EmpDisabled;
        _window.OutputButton.Disabled = smes.EmpDisabled;
        _window.InputMinButton.Disabled = smes.EmpDisabled || storage.InputLimit <= 0;
        _window.InputMaxButton.Disabled = smes.EmpDisabled || storage.InputLimit >= storage.MaxInput;
        _window.OutputMinButton.Disabled = smes.EmpDisabled || storage.OutputLimit <= 0;
        _window.OutputMaxButton.Disabled = smes.EmpDisabled || storage.OutputLimit >= storage.MaxOutput;
        _window.InputLimit.LineEditDisabled = smes.EmpDisabled;
        _window.OutputLimit.LineEditDisabled = smes.EmpDisabled;
        _window.InputLimit.SetButtonDisabled(smes.EmpDisabled);
        _window.OutputLimit.SetButtonDisabled(smes.EmpDisabled);
    }

    private void ToggleInput(bool enabled)
    {
        SendPredictedMessage(new RMCSmesSetInputEnabledBuiMsg(enabled));
        Refresh(inputEnabledOverride: enabled);
    }

    private void ToggleOutput(bool enabled)
    {
        SendPredictedMessage(new RMCSmesSetOutputEnabledBuiMsg(enabled));
        Refresh(outputEnabledOverride: enabled);
    }

    private void SetFlowState(RMCSmesComponent smes, RMCPowerStorageComponent storage)
    {
        if (_window == null)
            return;

        if (smes.EmpDisabled)
        {
            SetState(_window.FlowState, "rmc-smes-ui-controls-unavailable", RedColor);
        }
        else if (storage.CurrentOutput > 0.01f)
        {
            SetState(_window.FlowState, "rmc-smes-ui-discharging", OrangeColor);
        }
        else if (storage.CurrentInput > 0.01f)
        {
            SetState(_window.FlowState, "rmc-smes-ui-charging", GreenColor);
        }
        else
        {
            SetState(_window.FlowState, "rmc-smes-ui-standby", InactiveColor);
        }
    }

    private void SetInputState(RMCSmesComponent smes, RMCPowerStorageComponent storage, bool inputEnabled)
    {
        if (_window == null)
            return;

        if (smes.EmpDisabled || !inputEnabled)
        {
            SetState(_window.InputState, "rmc-smes-ui-off", RedColor);
            return;
        }

        if (smes.ChargePercentage >= 0.999f)
        {
            SetState(_window.InputState, "rmc-smes-ui-fully-charged", GreenColor);
            return;
        }

        var (locId, color) = storage.InputState switch
        {
            RMCPowerStorageInputState.Full => ("rmc-smes-ui-input-full", GreenColor),
            RMCPowerStorageInputState.Partial => ("rmc-smes-ui-input-partial", OrangeColor),
            RMCPowerStorageInputState.Off => ("rmc-smes-ui-not-charging", InactiveColor),
            _ => throw new ArgumentOutOfRangeException(nameof(storage.InputState), storage.InputState, null),
        };
        SetState(_window.InputState, locId, color);
    }

    private void SetOutputState(RMCSmesComponent smes, RMCPowerStorageComponent storage, bool outputEnabled)
    {
        if (_window == null)
            return;

        if (smes.EmpDisabled || !outputEnabled)
        {
            SetState(_window.OutputState, "rmc-smes-ui-off", RedColor);
        }
        else if (smes.Charge <= 0)
        {
            SetState(_window.OutputState, "rmc-smes-ui-no-charge", RedColor);
        }
        else if (storage.CurrentOutput > 0.01f)
        {
            SetState(_window.OutputState, "rmc-smes-ui-supplying", OrangeColor);
        }
        else
        {
            SetState(_window.OutputState, "rmc-smes-ui-standby", InactiveColor);
        }
    }

    private void SetState(Label label, string locId, Color color)
    {
        label.Text = Loc.GetString(locId);
        label.FontColorOverride = color;
    }

    private static string Number(float value)
    {
        return value.ToString("0.#");
    }

    private static string Power(float watts)
    {
        return $"{Number(watts / 1000)} kW";
    }
}
