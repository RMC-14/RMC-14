using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Projectile;

[Serializable, NetSerializable]
public sealed class XenoProjectilePredictedHitEvent(int id, NetEntity target, GameTick lastRealTick, GameTick tick, int substep) : EntityEventArgs
{
    public readonly int Id = id;
    public readonly NetEntity Target = target;
    public readonly GameTick LastRealTick = lastRealTick; // last update the client received from server
    public readonly GameTick Tick = tick; // tick the client predicted the collision
    public readonly int Substep = substep; // substep of tick the client predicted the collision
}
