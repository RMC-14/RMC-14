using Content.Shared._RMC14.Mortar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Mortar;

[UsedImplicitly]
public sealed class MortarBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private MortarWindow? _window;

    protected override void Open()
    {
        _window = this.CreateWindow<MortarWindow>();

        if (EntMan.TryGetComponent(Owner, out MortarComponent? mortar))
        {
            _window.TargetX.Value = mortar.Target.X;
            _window.TargetY.Value = mortar.Target.Y;
            _window.DialX.Value = mortar.Dial.X;
            _window.DialY.Value = mortar.Dial.Y;
        }

        _window.SetTargetButton.OnPressed += _ =>
            SendPredictedMessage(new MortarTargetBuiMsg((_window.TargetX.Value, _window.TargetY.Value)));

        _window.SetOffsetButton.OnPressed += _ =>
            SendPredictedMessage(new MortarDialBuiMsg((_window.DialX.Value, _window.DialY.Value)));
    }
}
