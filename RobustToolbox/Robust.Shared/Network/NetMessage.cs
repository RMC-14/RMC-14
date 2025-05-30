using System;
using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network
{
    /// <summary>
    /// The group the message belongs to, used for statistics and packet channels.
    /// </summary>
    public enum MsgGroups : byte
    {
        /// <summary>
        /// Error state, the message needs to set a different one.
        /// </summary>
        Error = 0,

        /// <summary>
        /// A core message, like connect, disconnect, and tick.
        /// </summary>
        Core,

        /// <summary>
        /// Entity message, for keeping entities in sync.
        /// </summary>
        Entity,

        /// <summary>
        /// A string message, for chat.
        /// </summary>
        String,

        /// <summary>
        /// A command message from client -> server.
        /// </summary>
        Command,

        /// <summary>
        /// ECS Events between the server and the client.
        /// </summary>
        EntityEvent,
    }

    /// <summary>
    /// A packet message that the NetManager sends/receives.
    /// </summary>
    public abstract class NetMessage
    {
        /// <summary>
        /// String identifier of the message type.
        /// </summary>
        public virtual string MsgName { get; }

        /// <summary>
        /// The group this message type belongs to.
        /// </summary>
        public virtual MsgGroups MsgGroup { get; }

        /// <summary>
        /// The channel that this message came in on.
        /// </summary>
        public INetChannel MsgChannel { get; set; } = default!;

        /// <summary>
        ///     The size of this packet in bytes.
        /// </summary>
        public int MsgSize { get; set; }

        protected NetMessage()
        {
            MsgName = GetType().Name;
        }

        /// <summary>
        /// Deserializes the NetIncomingMessage into this NetMessage class.
        /// </summary>
        /// <param name="buffer">The buffer of the raw incoming packet.</param>
        /// <param name="serializer"></param>
        public abstract void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer);

        /// <summary>
        /// Serializes this NetMessage into a new NetOutgoingMessage.
        /// </summary>
        /// <param name="buffer">The buffer of the new packet being serialized.</param>
        /// <param name="serializer"></param>
        public abstract void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer);

        public virtual NetDeliveryMethod DeliveryMethod
        {
            get
            {
                switch (MsgGroup)
                {
                    case MsgGroups.Entity:
                        return NetDeliveryMethod.Unreliable;
                    case MsgGroups.Core:
                    case MsgGroups.Command:
                        return NetDeliveryMethod.ReliableUnordered;
                    case MsgGroups.String:
                    case MsgGroups.EntityEvent:
                        return NetDeliveryMethod.ReliableOrdered;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
