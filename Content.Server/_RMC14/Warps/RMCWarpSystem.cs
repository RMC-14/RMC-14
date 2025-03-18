using Content.Server.Warps;
using Content.Shared._RMC14.Warps;

namespace Content.Server._RMC14.Warps;

public sealed class RMCWarpSystem : SharedRMCWarpSystem
{
    public override string? GetName(EntityUid warp)
    {
        return CompOrNull<WarpPointComponent>(warp)?.Location;
    }
}
