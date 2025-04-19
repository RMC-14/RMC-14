using Content.Shared.Database;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Commendations;

public sealed class CommendationsMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public List<Commendation> Commendations = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Commendations.Clear();

        var length = buffer.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var giver = buffer.ReadString();
            var receiver = buffer.ReadString();
            var name = buffer.ReadString();
            var text = buffer.ReadString();
            var type = (CommendationType) buffer.ReadInt32();
            var round = buffer.ReadInt32();
            Commendations.Add(new Commendation(giver, receiver, name, text, type, round));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Commendations.Count);
        foreach (var commendation in Commendations)
        {
            buffer.Write(commendation.Giver);
            buffer.Write(commendation.Receiver);
            buffer.Write(commendation.Name);
            buffer.Write(commendation.Text);
            buffer.Write((int) commendation.Type);
            buffer.Write(commendation.Round);
        }
    }
}
