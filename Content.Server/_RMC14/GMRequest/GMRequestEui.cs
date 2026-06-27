using Content.Server.Administration;
using Content.Server.EUI;
using Content.Server.Prayer;
using Content.Shared._RMC14.GMRequest;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._RMC14.GMRequest;

public sealed class GMRequestEui : BaseEui
{
    [Dependency] private readonly GMRequestManager _manager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public GMRequestEui()
    {
        IoCManager.InjectDependencies(this);

        _manager.NewLogReceived += id => SendLog(id, true);
        _manager.LogsCleared += SendLogs; //A necessary concession for the UI to update on round restart
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case GMRequestClaimMessage claimMessage:
                _manager.Claim(claimMessage.Id, claimMessage.Claimant);
                SendLog(claimMessage.Id, false);
                break;

            case GMRequestHideMessage hideMessage:
                _manager.Hide(hideMessage.Id);
                SendLog(hideMessage.Id, false);
                break;

            case GMRequestSubtleMessage subtleMessage:
                Subtle(subtleMessage.sender, subtleMessage.target);
                break;

            case GMRequestRequestLogs requestMessage:
                SendLogs();
                break;

            case GMRequestClearMessage clearMessage:
                _manager.ClearGMRequests();
                break;
        }
    }

    private void SendLogs()
    {
        SendMessage(new GMRequestSentLogsMessage()
        {
            Logs = _manager.Logs,
        });
    }

    private void SendLog(int id, bool sound)
    {
        SendMessage(new GMRequestSentLogMessage()
        {
            Id = id,
            Log = _manager.Logs[id],
            Sound = sound,
        });
    }

    //No reason to send this all the way to GMRequestManager, handle it here
    private void Subtle(NetUserId sender, NetUserId target)
    {
        if (_playerManager.TryGetSessionById(sender, out var SenderSession) &&
            _playerManager.TryGetSessionById(target, out var TargetSession))
        {
            var quickdialog = _entitySystemManager.GetEntitySystem<QuickDialogSystem>();
            quickdialog.OpenDialog(SenderSession,
                Loc.GetString("rmc-gm-request-subtle-ui-title"),
                Loc.GetString("rmc-gm-request-subtle-ui-message"),
                Loc.GetString("rmc-gm-request-subtle-ui-popup"),
                (LongString message, LongString popupMessage) =>
                {
                    var prayer = _entitySystemManager.GetEntitySystem<PrayerSystem>();
                    prayer.SendSubtleMessage(TargetSession,
                        SenderSession,
                        message,
                        popupMessage == "" ? Loc.GetString("rmc-gm-request-subtle-default") : popupMessage);
                });
        }
    }
}
