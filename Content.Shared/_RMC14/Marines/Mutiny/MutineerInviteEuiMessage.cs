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
