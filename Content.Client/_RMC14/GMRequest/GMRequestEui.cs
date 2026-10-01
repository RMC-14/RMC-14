using Content.Client.Administration.Managers;
using Content.Client.Eui;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.GMRequest;
using Content.Shared.Eui;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._RMC14.GMRequest;

public sealed class GMRequestEui : BaseEui
{
    [Dependency] private readonly GMRequestClientManager _manager = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntitySystemManager _entMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private readonly GMRequestWindow _window;
    private readonly ICommonSession? _session;
    private SoundSpecifier? _gmRequestSound;

    public GMRequestEui()
    {
        IoCManager.InjectDependencies(this);

        _window = new GMRequestWindow();
        _session = _playerManager.LocalSession;

        SendMessage(new GMRequestRequestLogs()); //Request logs on first load

        _window.ClaimLogButton.OnPressed += ClaimButtonPressed;
        _window.SubtleMessagePlayerButton.OnPressed += SubtleButtonPressed;

        _window.HideShowButton.OnPressed += HideButtonPressed;
        _window.RefreshButton.OnPressed += _ => SendMessage(new GMRequestRequestLogs());
        _window.DeleteGMRequestLogsButton.OnPressed += DeleteButtonPressed;

        _cfg.OnValueChanged(RMCCVars.RMCGMRequestSound, v => _gmRequestSound = new SoundPathSpecifier(v), true);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case GMRequestSentLogsMessage newLogs:
                if (newLogs.Logs != null)
                {
                    _manager.SetLogs(newLogs.Logs);
                    _window.Populate();
                }
                break;

            case GMRequestSentLogMessage sentLog:
                if (_adminManager.IsAdmin() && !_cfg.GetCVar(RMCCVars.RMCGMRequestSoundMuted) && sentLog.Sound)
                {
                    var audio = _entMan.GetEntitySystem<SharedAudioSystem>();
                        audio.PlayGlobal(_gmRequestSound, Filter.Local(), false);
                }

                _manager.AddLog(sentLog.Id, sentLog.Log);
                _window.Populate();
                break;

            case GMRequestClearMessage clearMessage:
                _manager.ClearLogs();
                break;
        }

    }

    public void ClaimButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (_window._selectedLog == null || _window._selectedLogId == null || _session == null) //checking both should be redundant but the compiler is yelling at me
            return;

        if (_window._selectedLog.Value.ClaimName != null)
        {
            SendMessage(new GMRequestClaimMessage()
            {
                Claimant = null,
                Id = _window._selectedLogId.Value,
            });
        }
        else
        {
            SendMessage(new GMRequestClaimMessage()
            {
                Claimant = _session.Name,
                Id = _window._selectedLogId.Value,
            });
        }
    }

    //Subtle can only be accessed through the serverside
    //prayer system, hence sending this message
    private void SubtleButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (_window._selectedLog == null || _session == null)
            return;

        SendMessage(new GMRequestSubtleMessage()
        {
            sender = _session.UserId,
            target = _window._selectedLog.Value.Sender,
        });
    }

    public void HideButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if(_window._selectedLogId == null)
            return;

        SendMessage(new GMRequestHideMessage()
        {
            Id = _window._selectedLogId.Value,
        });
    }

    public void DeleteButtonPressed(BaseButton.ButtonEventArgs args)
    {
        SendMessage(new GMRequestClearMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}
