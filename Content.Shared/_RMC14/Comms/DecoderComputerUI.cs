using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum DecoderComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DecoderComputerSubmitCodeMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class DecoderComputerQuickRestoreMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DecoderComputerPrintMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DecoderComputerRefillMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DecoderComputerGenerateMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DecoderComputerBuiState(
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
