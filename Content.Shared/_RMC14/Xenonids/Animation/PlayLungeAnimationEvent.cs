using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Animation;

[Serializable, NetSerializable]
public class PlayLungeAnimationEvent(NetEntity entityUid, Vector2 direction) : EntityEventArgs
{
    public NetEntity EntityUid { get; } = entityUid;
    public Vector2 Direction { get; } = direction;
}
