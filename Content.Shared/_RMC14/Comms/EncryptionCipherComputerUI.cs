using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum EncryptionCipherComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EncryptionCipherChangeSettingMsg(int delta) : BoundUserInterfaceMessage
{
    public readonly int Delta = delta;
}

[Serializable, NetSerializable]
public sealed class EncryptionCipherPrintOutputMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCipherPrintMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCipherSetInputMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class EncryptionCipherRefillMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCipherComputerBuiState(
    string inputCode,
    int cipherSetting,
    string decipheredWord,
    string statusMessage,
    int punchcardCount,
    bool validWord
) : BoundUserInterfaceState
{
    public readonly string InputCode = inputCode;
    public readonly int CipherSetting = cipherSetting;
    public readonly string DecipheredWord = decipheredWord;
    public readonly string StatusMessage = statusMessage;
    public readonly int PunchcardCount = punchcardCount;
    public readonly bool ValidWord = validWord;
}
