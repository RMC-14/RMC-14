using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.GameStates;

public sealed class SharedRMCGameStateSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

    /// <summary>
    /// Puts the game into "InSimulation" state. For whenever that's necessary.
    /// Remember to call Unenforce() or dispose this object. Or use `using var` to do so automatically.
    /// </summary>
    /// <remarks>
    /// Welcome to one of the most cursed workarounds I've written yet.
    /// It turns out that on the client, we do a lot of game actions "out of simulation". That's kind of bad.
    /// If we need to force the game into "in simulation" mode, do `using var enforcedSim = _rmcGameState.EnforceSimulation()`.
    /// The game will be put into simulation state, and when the var goes out of scope the simulation will return to its previous state.
    /// It only does anything on the client.
    /// </remarks>
    /// <returns>An EnforcedInSimulation object. Use with a `using var` so that it disposes automatically.</returns>
    public EnforcedInSimulation EnforceSimulation()
    {
        var result = new EnforcedInSimulation(_timing.InSimulation, _timing, _net);

        if (_net.IsClient)
            _timing.InSimulation = true;

        return result;
    }

    public readonly struct EnforcedInSimulation : IDisposable
    {
        public bool OldInSimulation { get; }
        private IGameTiming Timing { get; }
        private INetManager Net { get; }

        public EnforcedInSimulation(bool oldInSimulation, IGameTiming timing, INetManager net)
        {
            OldInSimulation = oldInSimulation;
            Timing = timing;
            Net = net;
        }

        public void Unenforce()
        {
            if (Net.IsClient)
                Timing.InSimulation = OldInSimulation;
        }

        // Yes we're disposing a struct so that we can make use of that
        // sweet `using var` syntactic sugar.
        public void Dispose()
        {
            Unenforce();
        }
    }
}
