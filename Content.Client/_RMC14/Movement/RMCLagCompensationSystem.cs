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
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IClientGameStateManager _gameState = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, CVars.NetBufferSize, _ => SendCVars(), true);
        Subs.CVar(_config, CVars.NetInterp, _ => SendCVars(), true);
    }

    private void SendCVars()
    {
        var ev = new RMCNetCVarsEvent(_gameState.TargetBufferSize);
        RaiseNetworkEvent(ev);
    }

    public override GameTick GetLastRealTick(NetUserId? session)
    {
        return _timing.LastRealTick;
    }
}
