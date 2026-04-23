using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor.ImaginaryFriend;

[Serializable, NetSerializable]
public sealed class BecomeImaginaryFriendMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}
