using Content.Server.EUI;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed class MutinyBeginEui(
    EntityUid leaderMind,
    EntityUid rule,
    MutinyRuleSystem mutiny) : BaseEui
{
    private bool _handled;

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (_handled)
            return;

        if (msg is MutinyBeginChoiceMessage { Accepted: true })
        {
            _handled = true;
            mutiny.TryBeginMutiny(leaderMind, rule, this);
            if (!IsShutDown)
                Close();
            return;
        }

        Cancel();
    }

    public override void Closed()
    {
        _handled = true;
        mutiny.OnBeginClosed(leaderMind, this);
    }

    public void Cancel()
    {
        if (_handled)
            return;

        _handled = true;
        mutiny.OnBeginClosed(leaderMind, this);
        if (!IsShutDown)
            Close();
    }
}
