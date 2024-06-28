using Content.Shared._RMC14.LinkAccount;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.LinkAccount;

public sealed class LinkDiscordAccountUIController : UIController
{
    [Dependency] private readonly IClipboardManager _clipboard = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private LinkDiscordWindow? _window;
    private TimeSpan _disableUntil;

    private Guid _code;

    public override void Initialize()
    {
        _net.RegisterNetMessage<LinkAccountCodeEvent>(OnCode);
        _net.RegisterNetMessage<LinkAccountRequestEvent>();
    }

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = new LinkDiscordWindow();
            _window.OnClose += () => _window = null;
            _window.Button.OnPressed += _ =>
            {
                _clipboard.SetText(_code.ToString());
                _window.Button.Text = Loc.GetString("rmc-ui-link-discord-account-copied");
                _window.Button.Disabled = true;
            };

            _window.OpenCentered();

            if (_code == default)
                _window.Button.Disabled = true;

            _net.ClientSendMessage(new LinkAccountRequestEvent());
            return;
        }

        _window.Close();
        _window = null;
    }

    private void OnCode(LinkAccountCodeEvent message)
    {
        _code = message.Code;

        if (_window == null)
            return;

        _window.Button.Disabled = false;
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
