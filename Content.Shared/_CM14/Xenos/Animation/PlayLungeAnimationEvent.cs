using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Animations;

[Serializable, NetSerializable]
public class PlayLungeAnimationEvent : EntityEventArgs
{
    public NetEntity EntityUid { get; } 
    public Vector2 Direction { get; } 

    public PlayLungeAnimationEvent(NetEntity entityUid, Vector2 direction)
    {
        EntityUid = entityUid;
        Direction = direction;
    }
}
