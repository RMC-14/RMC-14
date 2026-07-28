using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Mutiny;

[Serializable, NetSerializable]
public enum MutineerInviteUiButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class MutineerInviteChoiceMessage : EuiMessageBase
{
    public readonly MutineerInviteUiButton Button;

    public MutineerInviteChoiceMessage(MutineerInviteUiButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public sealed class MutinyBeginChoiceMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}

[Serializable, NetSerializable]
public sealed class MutinySideChoiceMessage(MutinySide side) : EuiMessageBase
{
    public readonly MutinySide Side = side;
}

[Serializable, NetSerializable]
public sealed class MutinySideEuiState(bool canJoinMutineers) : EuiStateBase
{
    public readonly bool CanJoinMutineers = canJoinMutineers;
}
