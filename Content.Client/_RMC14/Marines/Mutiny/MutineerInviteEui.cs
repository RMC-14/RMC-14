using Content.Client.Eui;
using Content.Shared._RMC14.Marines.Mutiny;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Marines.Mutiny;

[UsedImplicitly]
public sealed class MutineerInviteEui : BaseEui
{
    private readonly MutineerInviteWindow _window;

    public MutineerInviteEui()
    {
        _window = new MutineerInviteWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new MutineerInviteChoiceMessage(MutineerInviteUiButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new MutineerInviteChoiceMessage(MutineerInviteUiButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new MutineerInviteChoiceMessage(MutineerInviteUiButton.Accept));
            _window.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
