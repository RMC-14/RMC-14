using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Leap;

[Serializable, NetSerializable]
public sealed class XenoLeapPredictedHitEvent(NetEntity target, GameTick lastRealTick, int substep = 0) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly GameTick LastRealTick = lastRealTick;
    public readonly int Substep = substep;
}
