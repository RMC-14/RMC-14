using Content.Shared._RMC14.Movement;
using Robust.Client.GameStates;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Movement;

public sealed class RMCLagCompensationSystem : SharedRMCLagCompensationSystem
{
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override GameTick GetLastRealTick(NetUserId? session)
    {
        return _timing.LastRealTick;
    }
}
