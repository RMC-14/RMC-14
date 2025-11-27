using System.Numerics;
using Lidgren.Network;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

public sealed class MappingDragGrabMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public Box2 Box;
    public MapId Map;
    public Vector2 Offset;
    public bool SpaceSourceTiles;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Box = new Box2(buffer.ReadVector2(), buffer.ReadVector2());
        Map = new MapId(buffer.ReadInt32());
        Offset = buffer.ReadVector2();
        SpaceSourceTiles = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Box.BottomLeft);
        buffer.Write(Box.TopRight);
        buffer.Write((int) Map);
        buffer.Write(Offset);
        buffer.Write(SpaceSourceTiles);
    }
}
