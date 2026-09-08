using Content.Server.EUI;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Mentor.ImaginaryFriend;

public sealed class BecomeImaginaryFriendEui(ImaginaryFriendSystem friend, EntityUid targetEntity, ICommonSession session) : BaseEui
{
    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not BecomeImaginaryFriendMessage { Accepted: true } message ||
            session.AttachedEntity is not { } userEntity)
        {
            Close();
            return;
        }

        friend.BecomeImaginaryFriend(targetEntity, userEntity, message.DefaultCharacter);

        Close();
    }
}
