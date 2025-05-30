﻿using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    internal sealed class MsgLoginSuccess : NetMessage
    {
        // Same deal as MsgLogin, helper for NetManager only.
        public override string MsgName => string.Empty;

        public override MsgGroups MsgGroup => MsgGroups.Core;

        public NetUserData UserData;
        public LoginType Type;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var name = buffer.ReadString();
            var id = buffer.ReadGuid();
            var patreonTier = buffer.ReadString();
            if (patreonTier.Length == 0)
                patreonTier = null;

            UserData = new NetUserData(new NetUserId(id), name) {PatronTier = patreonTier};
            Type = (LoginType) buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(UserData.UserName);
            buffer.Write(UserData.UserId);
            buffer.Write(UserData.PatronTier);
            buffer.Write((byte) Type);
            buffer.Write(new byte[100]);
        }
    }
}
