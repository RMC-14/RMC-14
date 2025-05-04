using Content.Shared._RMC14.Mortar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Mortar;

[UsedImplicitly]
public sealed class MortarBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private MortarWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MortarWindow>();

        Refresh();

        static int Parse(FloatSpinBox spinBox)
        {
            return (int) spinBox.Value;
        }

        static void SetSpinBox(FloatSpinBox spinBox, int limit, int value)
        {
            spinBox.Value = value;
            spinBox.OnValueChanged += args =>
            {
                var value = Math.Clamp(args.Value, -limit, limit);
                spinBox.Value = value;
            };
        }

        if (EntMan.TryGetComponent(Owner, out MortarComponent? mortar))
        {
            SetSpinBox(_window.TargetX, mortar.MaxTarget, mortar.Target.X);
            SetSpinBox(_window.TargetY, mortar.MaxTarget, mortar.Target.Y);
            SetSpinBox(_window.DialX, mortar.MaxDial, mortar.Dial.X);
            SetSpinBox(_window.DialY, mortar.MaxDial, mortar.Dial.Y);
            _window.SetTargetButton.OnPressed += _ =>
                SendPredictedMessage(new MortarTargetBuiMsg((Parse(_window.TargetX), Parse(_window.TargetY))));

            _window.SetOffsetButton.OnPressed += _ =>
                SendPredictedMessage(new MortarDialBuiMsg((Parse(_window.DialX), Parse(_window.DialY))));
        }

        _window.ViewCameraButton.OnPressed += _ => SendPredictedMessage(new MortarViewCamerasMsg());
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out MortarComponent? mortar))
            return;

        static void SetValue(FloatSpinBox? spinBox, int value)
        {
            if (spinBox != null)
                spinBox.Value = value;
        }

        SetValue(_window.TargetX, mortar.Target.X);
        SetValue(_window.TargetY, mortar.Target.Y);
        SetValue(_window.DialX, mortar.Dial.X);
        SetValue(_window.DialY, mortar.Dial.Y);
        _window.MaxDialLabel.Text = Loc.GetString("rmc-mortar-offset-max", ("max", mortar.MaxDial));
    }
}
