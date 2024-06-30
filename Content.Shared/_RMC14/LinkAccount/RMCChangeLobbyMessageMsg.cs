using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

public sealed class RMCChangeLobbyMessageMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string? Text;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        if (buffer.PeekStringSize() > 10000)
            return;

        Text = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Text);
    }
}
