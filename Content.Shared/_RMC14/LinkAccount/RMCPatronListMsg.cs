using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

public sealed class RMCPatronListMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public List<SharedRMCPatron> Patrons = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Patrons = new List<SharedRMCPatron>(count);
        for (var i = 0; i < count; i++)
        {
            var name = buffer.ReadString();
            var tier = buffer.ReadString();
            Patrons.Add(new SharedRMCPatron(name, tier));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Patrons.Count);
        foreach (var patron in Patrons)
        {
            buffer.Write(patron.Name);
            buffer.Write(patron.Tier);
        }
    }
}
