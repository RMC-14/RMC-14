using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Marines.Mutiny;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Marines.Mutiny;

[UsedImplicitly]
public sealed class MutinyBeginEui : BaseEui
{
    private readonly ConfirmationWindow _window = new();
    private bool _handled;

    public MutinyBeginEui()
    {
        _window.Setup(
            Loc.GetString("rmc-mutiny-begin-title"),
            Loc.GetString("rmc-mutiny-begin-text"),
            Loc.GetString("rmc-mutiny-begin-accept"),
            Loc.GetString("rmc-mutiny-begin-deny"));

        _window.AcceptButton.OnPressed += _ => SendOnce(true);
        _window.DenyButton.OnPressed += _ => SendOnce(false);
        _window.OnClose += () => SendOnce(false, closeWindow: false);
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _handled = true;
        _window.Close();
    }

    private void SendOnce(bool accepted, bool closeWindow = true)
    {
        if (_handled)
            return;

        _handled = true;
        SendMessage(new MutinyBeginChoiceMessage(accepted));
        if (closeWindow)
            _window.Close();
    }
}
