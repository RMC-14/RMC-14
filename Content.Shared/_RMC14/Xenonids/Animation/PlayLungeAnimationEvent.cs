using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Animation;

[Serializable, NetSerializable]
public class PlayLungeAnimationEvent(NetEntity entityUid, Vector2 direction, bool client) : EntityEventArgs
{
    public readonly NetEntity EntityUid = entityUid;
    public readonly Vector2 Direction = direction;
    public readonly bool Client = client;
}
