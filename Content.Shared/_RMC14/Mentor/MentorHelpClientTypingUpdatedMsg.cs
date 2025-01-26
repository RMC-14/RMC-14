using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

public sealed class MentorHelpClientTypingUpdatedMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public Guid Destination;
    public bool Typing;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Destination = buffer.ReadGuid();
        Typing = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Destination);
        buffer.Write(Typing);
    }
}
