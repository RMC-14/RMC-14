using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Lunge;

[Serializable, NetSerializable]
public sealed class XenoLungePredictedHitEvent(NetEntity target, GameTick lastRealTick) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly GameTick LastRealTick = lastRealTick;
}
