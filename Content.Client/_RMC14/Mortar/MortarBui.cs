using Content.Shared._RMC14.Mortar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Mortar;

[UsedImplicitly]
public sealed class MortarBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private MortarWindow? _window;
    private FloatSpinBox? _targetX;
    private FloatSpinBox? _targetY;
    private FloatSpinBox? _dialX;
    private FloatSpinBox? _dialY;

    protected override void Open()
    {
        _window = this.CreateWindow<MortarWindow>();

        Refresh();

        static int Parse(FloatSpinBox spinBox)
        {
            return (int) spinBox.Value;
        }

        static FloatSpinBox CreateSpinBox(BoxContainer container, int limit, int value)
        {
            var spinBox = new FloatSpinBox(1, 0) { MinWidth = 130 };
            spinBox.Value = value;
            spinBox.OnValueChanged += args =>
            {
                var value = Math.Clamp(args.Value, -limit, limit);
                spinBox.Value = value;
            };

            container.AddChild(spinBox);
            return spinBox;
        }

        if (EntMan.TryGetComponent(Owner, out MortarComponent? mortar))
        {
            _targetX = CreateSpinBox(_window.TargetXContainer, mortar.MaxTarget, mortar.Target.X);
            _targetY = CreateSpinBox(_window.TargetYContainer, mortar.MaxTarget, mortar.Target.Y);
            _dialX = CreateSpinBox(_window.DialXContainer, mortar.MaxDial, mortar.Dial.X);
            _dialY = CreateSpinBox(_window.DialYContainer, mortar.MaxDial, mortar.Dial.Y);

            _window.SetTargetButton.OnPressed += _ =>
                SendPredictedMessage(new MortarTargetBuiMsg((Parse(_targetX), Parse(_targetY))));

            _window.SetOffsetButton.OnPressed += _ =>
                SendPredictedMessage(new MortarDialBuiMsg((Parse(_dialX), Parse(_dialY))));
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

        SetValue(_targetX, mortar.Target.X);
        SetValue(_targetY, mortar.Target.Y);
        SetValue(_dialX, mortar.Dial.X);
        SetValue(_dialY, mortar.Dial.Y);
        _window.MaxDialLabel.Text = Loc.GetString("rmc-mortar-offset-max", ("max", mortar.MaxDial));
    }
}
