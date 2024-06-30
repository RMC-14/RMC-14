using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PlayTimeTracking;

public sealed class RMCExcludedTimersMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public HashSet<string> Trackers = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Trackers = new HashSet<string>(count);
        for (var i = 0; i < count; i++)
        {
            Trackers.Add(buffer.ReadString());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Trackers.Count);
        foreach (var tracker in Trackers)
        {
            buffer.Write(tracker);
        }
    }
}
