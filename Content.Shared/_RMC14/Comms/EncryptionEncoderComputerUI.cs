using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum EncryptionEncoderComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EncryptionEncoderComputerSubmitCodeMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class EncryptionEncoderComputerPrintMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionEncoderComputerRefillMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionEncoderComputerGenerateMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionEncoderComputerBuiState(
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
public sealed class EncryptionEncoderChangeOffsetMsg(int delta) : BoundUserInterfaceMessage
{
    public readonly int Delta = delta;
}

