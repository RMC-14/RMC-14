using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum EncryptionCoderComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerSubmitCodeMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerQuickRestoreMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerPrintMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerRefillMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerGenerateMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerBuiState(
    string lastSubmittedCode,
    int knownLetters,
    string clarityDescription,
    string currentWord,
    int currentOffset
) : BoundUserInterfaceState
{
    public readonly string LastSubmittedCode = lastSubmittedCode;
    public readonly int KnownLetters = knownLetters;
    public readonly string ClarityDescription = clarityDescription;
    public readonly string CurrentWord = currentWord;
    public readonly int CurrentOffset = currentOffset;
}

[Serializable, NetSerializable]
public sealed class EncryptionCoderChangeOffsetMsg(int delta) : BoundUserInterfaceMessage
{
    public readonly int Delta = delta;
}
