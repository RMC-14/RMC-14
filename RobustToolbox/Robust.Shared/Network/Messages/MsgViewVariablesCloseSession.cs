using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent from client to server or server to client to notify to close a session.
    /// </summary>
    public sealed class MsgViewVariablesCloseSession : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     The session ID to close, which was agreed upon in <see cref="MsgViewVariablesOpenSession.SessionId"/>.
        /// </summary>
        public uint SessionId { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            SessionId = buffer.ReadUInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(SessionId);
        }
    }
}
