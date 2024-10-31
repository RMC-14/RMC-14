using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

public sealed class RMCChangeGhostColorMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Color Color;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Color = buffer.ReadColor();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Color);
    }
}
