using JetBrains.Annotations;
using Content.Shared._RMC14.Comms;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Comms;

[UsedImplicitly]
public sealed class EncryptionCoderComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private EncryptionCoderComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<EncryptionCoderComputerWindow>();
        _window.ChangeOffset += delta => SendPredictedMessage(new EncryptionCoderChangeOffsetMsg(delta));
        _window.SubmitCode += () => SendPredictedMessage(new EncryptionCoderComputerSubmitCodeMsg(""));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not EncryptionCoderComputerBuiState s || _window == null)
            return;

        _window.LastSubmittedCode = s.LastSubmittedCode;
        _window.KnownLetters = s.KnownLetters;
        _window.ClarityDescription = s.ClarityDescription;
        _window.CurrentWord = s.CurrentWord;
        _window.CurrentOffset = s.CurrentOffset;
    }
}
