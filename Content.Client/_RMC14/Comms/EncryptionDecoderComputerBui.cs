using JetBrains.Annotations;
using Content.Shared._RMC14.Comms;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Comms;

[UsedImplicitly]
public sealed class EncryptionDecoderComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private EncryptionDecoderComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<EncryptionDecoderComputerWindow>();
        _window.SubmitCode += code => SendPredictedMessage(new EncryptionDecoderComputerSubmitCodeMsg(code));
        _window.Print += () => SendPredictedMessage(new EncryptionDecoderComputerPrintMsg());
        _window.Refill += () => SendPredictedMessage(new EncryptionDecoderComputerRefillMsg());
        _window.Generate += () => SendPredictedMessage(new EncryptionDecoderComputerGenerateMsg());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not EncryptionDecoderComputerBuiState s || _window == null)
            return;

        _window.CurrentChallengeCode = s.CurrentChallengeCode;
        _window.HasGracePeriod = s.HasGracePeriod;
        _window.GracePeriodEnd = s.GracePeriodEnd;
        _window.StatusMessage = s.StatusMessage;
        _window.PunchcardCount = s.PunchcardCount;
    }
}

