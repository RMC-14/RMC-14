using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Projectile;

[RegisterComponent]
public sealed partial class XenoClientProjectileShotComponent : Component
{
    public GameTick LatestPredictedTick;
}
