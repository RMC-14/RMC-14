using Content.Server.EUI;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed class MutineerInviteEui(
    EntityUid leaderMind,
    EntityUid targetMind,
    EntityUid rule,
    MutinyRuleSystem mutiny) : BaseEui
{
    private bool _handled;

    public EntityUid LeaderMind => leaderMind;

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (_handled)
            return;

        if (msg is MutineerInviteChoiceMessage { Button: MutineerInviteUiButton.Accept })
        {
            _handled = true;
            mutiny.TryAcceptRecruit(leaderMind, targetMind, rule, this);
            if (!IsShutDown)
                Close();
            return;
        }

        Cancel();
    }

    public override void Closed()
    {
        _handled = true;
        mutiny.OnInviteClosed(targetMind, this);
    }

    public void Cancel()
    {
        if (_handled)
            return;

        _handled = true;
        mutiny.OnInviteClosed(targetMind, this);
        if (!IsShutDown)
            Close();
    }
}
