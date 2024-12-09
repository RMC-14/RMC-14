using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

public sealed class MentorSendMessageMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public string Message = string.Empty;
    public Guid To;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Message = buffer.ReadString();
        To = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Message);
        buffer.Write(To);
    }
}
