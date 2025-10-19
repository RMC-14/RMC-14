using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Destroy;

[Serializable, NetSerializable]
public sealed class XenoDestroyLeapStartEvent(NetEntity king, Vector2 leapOffset) : EntityEventArgs
{
    public readonly NetEntity King = king;

    public readonly Vector2 LeapOffset = leapOffset;
}
