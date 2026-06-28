using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids;

/// <summary>
/// Sent server -> client to inform the client of their total successful parasite infections.
/// </summary>
public sealed class RMCParasiteInfectionsMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public int Infections;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Infections = buffer.ReadVariableInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Infections);
    }
}
