using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Shared._RMC14.LinkAccount;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.LinkAccount;

public sealed class LinkAccountUIController : UIController
{
    [Dependency] private readonly IClipboardManager _clipboard = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private LinkAccountWindow? _window;
    private TimeSpan _disableUntil;

    private Guid _code;

    public const string ButtonName = "RMCLinkAccountButton";

    public override void Initialize()
    {
        _net.RegisterNetMessage<LinkAccountCodeMsg>(OnCode);
    }

    private void OnCode(LinkAccountCodeMsg message)
    {
        _code = message.Code;

        if (_window == null)
            return;

        _window.Button.Disabled = false;
    }

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = new LinkAccountWindow();
            _window.OnClose += () => _window = null;
            _window.Label.SetMarkupPermissive($"{Loc.GetString("rmc-ui-link-discord-account-text")}");
            if (_linkAccount.Linked)
                _window.Label.SetMarkupPermissive($"{Loc.GetString("rmc-ui-link-discord-account-already-linked")}\n\n{Loc.GetString("rmc-ui-link-discord-account-text")}");

            _window.Button.OnPressed += _ =>
            {
                _clipboard.SetText(_code.ToString());
                _window.Button.Text = Loc.GetString("rmc-ui-link-discord-account-copied");
                _window.Button.Disabled = true;
                _disableUntil = _timing.RealTime.Add(TimeSpan.FromSeconds(3));
            };

            _window.OpenCentered();

            if (_code == default)
                _window.Button.Disabled = true;

            _net.ClientSendMessage(new LinkAccountRequestMsg());
            return;
        }

        _window.Close();
        _window = null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_window == null)
            return;

        var time = _timing.RealTime;
        if (_disableUntil != default && time > _disableUntil)
        {
            _disableUntil = default;
            _window.Button.Text = Loc.GetString("rmc-ui-link-discord-account-copy");
            _window.Button.Disabled = false;
        }
    }
}
