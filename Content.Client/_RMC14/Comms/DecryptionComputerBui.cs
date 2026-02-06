using JetBrains.Annotations;
using Content.Shared._RMC14.Comms;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Comms;

[UsedImplicitly]
public sealed class DecryptionComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private DecryptionComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<DecryptionComputerWindow>();
        _window.SubmitCode += code => SendPredictedMessage(new DecryptionComputerSubmitCodeMsg(code));
        _window.QuickRestore += () => SendPredictedMessage(new DecryptionComputerQuickRestoreMsg());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not DecryptionComputerBuiState s || _window == null)
            return;

        _window.CurrentChallengeCode = s.CurrentChallengeCode;
        _window.HasGracePeriod = s.HasGracePeriod;
        _window.GracePeriodEnd = s.GracePeriodEnd;
        _window.StatusMessage = s.StatusMessage;
    }
}
