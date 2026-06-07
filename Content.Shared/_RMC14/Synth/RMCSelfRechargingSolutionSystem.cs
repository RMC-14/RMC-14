using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Runs fixed-interval solution refills for tools such as experimental welders.
/// </summary>
public sealed class RMCSelfRechargingSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSelfRechargingSolutionComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCSelfRechargingSolutionComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextRecharge = _timing.CurTime + ent.Comp.RechargeEvery;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCSelfRechargingSolutionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextRecharge)
                continue;

            comp.NextRecharge = _timing.CurTime + comp.RechargeEvery;

            if (!_solution.TryGetSolution(uid, comp.SolutionId, out var solution, out _))
                continue;

            // Only add what the solution can still hold so normal solution capacity rules remain authoritative.
            var amount = FixedPoint2.Min(comp.RechargeAmount, solution.Value.Comp.Solution.AvailableVolume);
            if (amount <= FixedPoint2.Zero)
                continue;

            _solution.TryAddReagent(solution.Value, comp.Reagent, amount, out _);
        }
    }
}
