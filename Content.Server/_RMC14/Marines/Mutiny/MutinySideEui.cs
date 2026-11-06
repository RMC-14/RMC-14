using Content.Server.EUI;
using Content.Shared._RMC14.Marines.Mutiny;
using Content.Shared.Eui;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed class MutinySideEui(
    EntityUid mind,
    EntityUid rule,
    bool canJoinMutineers,
    MutinyRuleSystem mutiny) : BaseEui
{
    private bool _handled;

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new MutinySideEuiState(canJoinMutineers);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (_handled)
            return;

        if (msg is MutinySideChoiceMessage choice &&
            (canJoinMutineers || choice.Side != MutinySide.Mutineer) &&
            mutiny.TryChooseSide(mind, rule, choice.Side, this))
        {
            _handled = true;
            if (!IsShutDown)
                Close();
            return;
        }

        if (msg is CloseEuiMessage)
            ResolveDefault();
    }

    public override void Closed()
    {
        if (!_handled)
        {
            _handled = true;
            mutiny.TryChooseSide(mind, rule, MutinySide.NonCombatant, this);
        }

        mutiny.OnSideChoiceClosed(mind, this);
    }

    public void ResolveDefault()
    {
        if (_handled)
            return;

        _handled = true;
        mutiny.TryChooseSide(mind, rule, MutinySide.NonCombatant, this);
        if (!IsShutDown)
            Close();
    }

    public void CancelWithoutChoice()
    {
        if (_handled)
            return;

        _handled = true;
        mutiny.OnSideChoiceClosed(mind, this);
        if (!IsShutDown)
            Close();
    }
}
