using Content.Shared.Database;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Commendations;

public sealed class CommendationsMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public List<Commendation> CommendationsReceived = new();
    public List<Commendation> CommendationsGiven = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        CommendationsReceived.Clear();
        CommendationsGiven.Clear();

        ReadCommendations(buffer, CommendationsReceived);
        ReadCommendations(buffer, CommendationsGiven);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        WriteCommendations(buffer, CommendationsReceived);
        WriteCommendations(buffer, CommendationsGiven);
    }

    private void ReadCommendations(NetIncomingMessage buffer, List<Commendation> commendations)
    {
        var length = buffer.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var giver = buffer.ReadString();
            var receiver = buffer.ReadString();
            var name = buffer.ReadString();
            var text = buffer.ReadString();
            var type = (CommendationType) buffer.ReadInt32();
            var round = buffer.ReadInt32();
            commendations.Add(new Commendation(giver, receiver, name, text, type, round));
        }
    }

    private void WriteCommendations(NetOutgoingMessage buffer, List<Commendation> commendations)
    {
        buffer.Write(commendations.Count);
        foreach (var commendation in commendations)
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
