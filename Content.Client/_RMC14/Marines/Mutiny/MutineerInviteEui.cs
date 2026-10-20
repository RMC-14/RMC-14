using Content.Client._RMC14.UserInterface;
using Content.Client.Eui;
using Content.Shared._RMC14.Marines.Mutiny;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Marines.Mutiny;

[UsedImplicitly]
public sealed class MutineerInviteEui : BaseEui
{
    private readonly ConfirmationWindow _window = new();
    private bool _handled;

    public MutineerInviteEui()
    {
        _window.Setup(
            Loc.GetString("mutineer-invite-title"),
            Loc.GetString("mutineer-invite-text"),
            Loc.GetString("mutineer-invite-accept"),
            Loc.GetString("mutineer-invite-deny"));

        _window.AcceptButton.OnPressed += _ => SendOnce(MutineerInviteUiButton.Accept);
        _window.DenyButton.OnPressed += _ => SendOnce(MutineerInviteUiButton.Deny);
        _window.OnClose += () => SendOnce(MutineerInviteUiButton.Deny, closeWindow: false);
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

    private void SendOnce(MutineerInviteUiButton button, bool closeWindow = true)
    {
        if (_handled)
            return;

        _handled = true;
        SendMessage(new MutineerInviteChoiceMessage(button));
        if (closeWindow)
            _window.Close();
    }
}
