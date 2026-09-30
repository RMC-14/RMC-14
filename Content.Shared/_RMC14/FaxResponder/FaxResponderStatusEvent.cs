using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.FaxResponder;

public sealed class FaxResponderStatusEvent : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int WeyaSlots;
    public int FreePressSlots;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        WeyaSlots = buffer.ReadVariableInt32();
        FreePressSlots = buffer.ReadVariableInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(WeyaSlots);
        buffer.WriteVariableInt32(FreePressSlots);
    }
}
