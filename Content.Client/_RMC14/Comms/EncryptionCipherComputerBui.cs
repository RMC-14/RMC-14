using JetBrains.Annotations;
using Content.Shared._RMC14.Comms;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Comms;

[UsedImplicitly]
public sealed class EncryptionCipherComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private EncryptionCipherComputerWindow? _window;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<EncryptionCipherComputerWindow>();
        _window.ChangeSetting += delta => SendPredictedMessage(new EncryptionCipherChangeSettingMsg(delta));
        _window.Print += () => SendPredictedMessage(new EncryptionCipherPrintMsg());
        _window.Refill += () => SendPredictedMessage(new EncryptionCipherRefillMsg());
        _window.ShiftLetter += (index, delta) => SendPredictedMessage(new EncryptionCipherShiftLetterMsg(index, delta));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not EncryptionCipherComputerBuiState s || _window == null)
            return;

        _window.InputCode = s.InputCode;
        _window.CipherSetting = s.CipherSetting;
        _window.DecipheredWord = s.DecipheredWord;
        _window.StatusMessage = s.StatusMessage;
        _window.PunchcardCount = s.PunchcardCount;
        _window.ValidWord = s.ValidWord;
    }
}
