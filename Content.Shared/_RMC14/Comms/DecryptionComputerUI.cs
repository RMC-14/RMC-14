using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Comms;

[Serializable, NetSerializable]
public enum DecryptionComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DecryptionComputerSubmitCodeMsg(string code) : BoundUserInterfaceMessage
{
    public readonly string Code = code;
}

[Serializable, NetSerializable]
public sealed class DecryptionComputerQuickRestoreMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DecryptionComputerBuiState(
    string currentChallengeCode,
    bool hasGracePeriod,
    TimeSpan gracePeriodEnd,
    string statusMessage
) : BoundUserInterfaceState
{
    public readonly string CurrentChallengeCode = currentChallengeCode;
    public readonly bool HasGracePeriod = hasGracePeriod;
    public readonly TimeSpan GracePeriodEnd = gracePeriodEnd;
    public readonly string StatusMessage = statusMessage;
}
