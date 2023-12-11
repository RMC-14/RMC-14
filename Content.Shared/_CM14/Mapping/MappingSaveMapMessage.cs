using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Mapping;

public sealed class MappingSaveMapMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string Path = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Path = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Path);
    }
}
