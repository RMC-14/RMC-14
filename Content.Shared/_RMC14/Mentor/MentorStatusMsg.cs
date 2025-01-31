using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

public sealed class MentorStatusMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public bool IsMentor;
    public bool CanReMentor;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsMentor = buffer.ReadBoolean();
        CanReMentor = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsMentor);
        buffer.Write(CanReMentor);
    }
}
