using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Timing;

public sealed class RMCTimingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Returns what the CurTime will be in simulation, even if the game
    /// state is currently out of simulation.
    /// </summary>
    /// <remarks>Used for code that runs out of simulation but needs to predict things
    /// that will run in simulation.</remarks>
    /// <returns></returns>
    public TimeSpan GetInSimulationTime()
    {
        // When we're not in simulation, CurTime might be a value that's between ticks. For
        // code that is trying to predict in simulation results, that isn't acceptable.
        // We can temporarily set InSimulation to true, which changes how CurTime is calculated
        // so that it will match what it WILL be during the real InSimulation portion, allowing
        // out of simulation code to properly predict the time.
        var oldInSim = _timing.InSimulation;
        _timing.InSimulation = true;

        var inSimTime = _timing.CurTime;

        _timing.InSimulation = oldInSim;

        return inSimTime;
    }
}
