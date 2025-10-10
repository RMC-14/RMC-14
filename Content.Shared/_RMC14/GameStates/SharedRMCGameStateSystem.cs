using Content.Shared.Administration;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.GameStates;

public sealed class SharedRMCGameStateSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RMCSetPredictionEvent>(OnDisablePrediction);
    }

    private void OnDisablePrediction(RMCSetPredictionEvent ev)
    {
        if (_net.IsServer)
            return;

        _config.SetCVar(CVars.NetPredict, ev.Enable);
    }
}
