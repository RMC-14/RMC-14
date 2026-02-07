using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum EncryptionCipherComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EncryptionCipherSetInputMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class EncryptionCipherChangeSettingMsg(int delta) : BoundUserInterfaceMessage
{
    public readonly int Delta = delta;
}

[Serializable, NetSerializable]
public sealed class EncryptionCipherPrintOutputMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCipherComputerBuiState(
    string inputCode,
    int cipherSetting,
    string decipheredWord,
    string statusMessage
) : BoundUserInterfaceState
{
    public readonly string InputCode = inputCode;
    public readonly int CipherSetting = cipherSetting;
    public readonly string DecipheredWord = decipheredWord;
    public readonly string StatusMessage = statusMessage;
}
