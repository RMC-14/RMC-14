using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Parasite;

public sealed class XenoNestedHostSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    private readonly List<ReagentId> _drain = new();
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (time < _nextUpdate)
            return;

        _nextUpdate = time + UpdateInterval;

        var query = EntityQueryEnumerator<VictimInfectedComponent, XenoNestedComponent>();
        while (query.MoveNext(out var uid, out var infected, out _))
        {
            if (infected.SpawnedLarva != null || _mobState.IsDead(uid) || infected.BurstDelay <= TimeSpan.Zero)
                continue;

            var progress = (infected.BurstDelay - (infected.BurstAt - time)) / infected.BurstDelay;
            if (progress >= infected.NestedStabilizerMaxProgress)
                continue;

            if (!_bloodstream.TryGetChemicalSolution(uid, out var solutionEnt, out var solution))
                continue;

            if (solution.GetTotalPrototypeQuantity(infected.NestedStabilizerReagent) < infected.NestedStabilizerMinimum)
                _solution.TryAddReagent(solutionEnt, infected.NestedStabilizerReagent, infected.NestedStabilizerMinimum);

            _drain.Clear();
            foreach (var reagent in solution.Contents)
            {
                if (_rmcReagent.IsGenerated(reagent.Reagent.Prototype))
                    _drain.Add(reagent.Reagent);
            }

            foreach (var reagent in _drain)
            {
                _solution.RemoveReagent(solutionEnt, reagent, infected.NestedGeneratedDrain);
            }
        }
    }
}
