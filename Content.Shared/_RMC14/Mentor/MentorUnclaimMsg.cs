using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

public sealed class MentorUnclaimMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public string Author = string.Empty;
    public Guid Destination;
    public bool Disconnect;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Author = buffer.ReadString();
        Destination = buffer.ReadGuid();
        Disconnect = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Author);
        buffer.Write(Destination);
        buffer.Write(Disconnect);
    }
}
