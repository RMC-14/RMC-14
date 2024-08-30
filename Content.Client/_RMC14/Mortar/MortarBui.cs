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
        _window = this.CreateWindow<MortarWindow>();

        Refresh();

        static int Parse(FloatSpinBox spinBox)
        {
            return (int) spinBox.Value;
        }

        _window.SetTargetButton.OnPressed += _ =>
            SendPredictedMessage(new MortarTargetBuiMsg((Parse(_window.TargetX), Parse(_window.TargetY))));

        _window.SetOffsetButton.OnPressed += _ =>
            SendPredictedMessage(new MortarDialBuiMsg((Parse(_window.DialX), Parse(_window.DialY))));

        _window.ViewCameraButton.OnPressed += _ => SendPredictedMessage(new MortarViewCamerasMsg());
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out MortarComponent? mortar))
            return;

        _window.TargetX.Value = mortar.Target.X;
        _window.TargetY.Value = mortar.Target.Y;
        _window.DialX.Value = mortar.Dial.X;
        _window.DialY.Value = mortar.Dial.Y;
        _window.MaxDialLabel.Text = Loc.GetString("rmc-mortar-offset-max", ("max", mortar.MaxDial));
    }
}
