using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum EncryptionCoderComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EncryptionCoderSubmitCodeMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class EncryptionCoderComputerBuiState(
    string lastSubmittedCode,
    int knownLetters,
    string clarityDescription
) : BoundUserInterfaceState
{
    public readonly string LastSubmittedCode = lastSubmittedCode;
    public readonly int KnownLetters = knownLetters;
    public readonly string ClarityDescription = clarityDescription;
}
