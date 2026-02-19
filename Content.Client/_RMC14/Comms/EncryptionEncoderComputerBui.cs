using JetBrains.Annotations;
using Content.Shared._RMC14.Comms;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Comms;

[UsedImplicitly]
public sealed class EncryptionEncoderComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private EncryptionEncoderComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<EncryptionEncoderComputerWindow>();
        _window.ChangeOffset += delta => SendPredictedMessage(new EncryptionEncoderChangeOffsetMsg(delta));
        _window.SubmitCode += () => SendPredictedMessage(new EncryptionEncoderComputerSubmitCodeMsg(""));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not EncryptionEncoderComputerBuiState s || _window == null)
            return;

        _window.LastSubmittedCode = s.LastSubmittedCode;
        _window.KnownLetters = s.KnownLetters;
        _window.CurrentWord = s.CurrentWord;
        _window.CurrentOffset = s.CurrentOffset;
    }
}

