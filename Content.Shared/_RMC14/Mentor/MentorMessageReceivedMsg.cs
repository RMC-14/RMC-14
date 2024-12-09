using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

public sealed class MentorMessagesReceivedMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public List<MentorMessage> Messages = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();
        Messages.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            var destination = new NetUserId(buffer.ReadGuid());
            var destinationName = buffer.ReadString();
            var author = new NetUserId(buffer.ReadGuid());
            var authorName = buffer.ReadString();
            var text = buffer.ReadString();
            var time = DateTime.FromBinary(buffer.ReadInt64());
            var isMentor = buffer.ReadBoolean();
            var message = new MentorMessage(destination, destinationName, author, authorName, text, time, isMentor);
            Messages.Add(message);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Messages.Count);
        foreach (var message in Messages)
        {
            buffer.Write(message.Destination.UserId);
            buffer.Write(message.DestinationName);
            buffer.Write(message.Author.UserId);
            buffer.Write(message.AuthorName);
            buffer.Write(message.Text);
            buffer.Write(message.Time.ToBinary());
            buffer.Write(message.IsMentor);
        }
    }
}
