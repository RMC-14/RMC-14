using JetBrains.Annotations;
using Content.Shared._RMC14.Comms;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Comms;

[UsedImplicitly]
public sealed class DecoderComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private DecoderComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<DecoderComputerWindow>();
        _window.SubmitCode += code => SendPredictedMessage(new DecoderComputerSubmitCodeMsg(code));
        _window.QuickRestore += () => SendPredictedMessage(new DecoderComputerQuickRestoreMsg());
        _window.Print += () => SendPredictedMessage(new DecoderComputerPrintMsg());
        _window.Refill += () => SendPredictedMessage(new DecoderComputerRefillMsg());
        _window.Generate += () => SendPredictedMessage(new DecoderComputerGenerateMsg());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not DecoderComputerBuiState s || _window == null)
            return;

        _window.CurrentChallengeCode = s.CurrentChallengeCode;
        _window.HasGracePeriod = s.HasGracePeriod;
        _window.GracePeriodEnd = s.GracePeriodEnd;
        _window.StatusMessage = s.StatusMessage;
        _window.PunchcardCount = s.PunchcardCount;
    }
}
