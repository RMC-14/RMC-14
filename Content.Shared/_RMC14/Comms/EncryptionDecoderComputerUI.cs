using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum EncryptionDecoderComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EncryptionDecoderComputerSubmitCodeMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class EncryptionDecoderComputerPrintMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionDecoderComputerRefillMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionDecoderComputerGenerateMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class EncryptionDecoderComputerBuiState(
    string currentChallengeCode,
    bool hasGracePeriod,
    TimeSpan gracePeriodEnd,
    string statusMessage,
    int punchcardCount
) : BoundUserInterfaceState
{
    public readonly string CurrentChallengeCode = currentChallengeCode;
    public readonly bool HasGracePeriod = hasGracePeriod;
    public readonly TimeSpan GracePeriodEnd = gracePeriodEnd;
    public readonly string StatusMessage = statusMessage;
    public readonly int PunchcardCount = punchcardCount;
}

