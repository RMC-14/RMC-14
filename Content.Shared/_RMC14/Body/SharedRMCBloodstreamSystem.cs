using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Body;

public abstract class SharedRMCBloodstreamSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private readonly List<ReagentId> _reagentsToRemove = new();

    public virtual bool TryGetBloodSolution(EntityUid uid, [NotNullWhen(true)] out Solution? solution)
    {
        // TODO RMC14
        return _solution.TryGetSolution(uid, "bloodstream", out _, out solution);
    }

    public virtual bool TryGetChemicalSolution(EntityUid uid, out Entity<SolutionComponent> solutionEnt, [NotNullWhen(true)] out Solution? solution)
    {
        // TODO RMC14
        solutionEnt = default;
        if (!_solution.TryGetSolution(uid, "chemicals", out var nullableSolutionEnt, out solution))
            return false;

        solutionEnt = nullableSolutionEnt.Value;
        return true;
    }

    public virtual bool IsBleeding(EntityUid uid)
    {
        // TODO RMC14
        return false;
    }

    public void RemoveBloodstreamToxins(EntityUid body, FixedPoint2 amount)
    {
        if (!TryGetChemicalSolution(body, out var solutionEnt, out _))
            return;

        _reagentsToRemove.Clear();
        foreach (var content in solutionEnt.Comp.Solution.Contents)
        {
            if (!_rmcReagent.TryIndex(content.Reagent, out var reagent))
                continue;

            if (!reagent.Toxin)
                continue;

            _reagentsToRemove.Add(content.Reagent);
        }

        foreach (var remove in _reagentsToRemove)
        {
            _solution.RemoveReagent(solutionEnt, remove, amount);
        }
    }

    public void RemoveBloodstreamChemical(EntityUid body, ProtoId<ReagentPrototype> reagentId, FixedPoint2 amount)
    {
        if (!TryGetChemicalSolution(body, out var solutionEnt, out _))
            return;

        _solution.RemoveReagent(solutionEnt, reagentId, amount);
    }

    public bool RemoveBloodstreamAlcohols(EntityUid body, FixedPoint2 amount)
    {
        if (!TryGetChemicalSolution(body, out var solutionEnt, out _))
            return false;

        _reagentsToRemove.Clear();
        foreach (var content in solutionEnt.Comp.Solution.Contents)
        {
            if (!_rmcReagent.TryIndex(content.Reagent, out var reagent))
                continue;

            if (!reagent.Alcohol)
                continue;

            _reagentsToRemove.Add(content.Reagent);
        }

        var alcoholRemoved = _reagentsToRemove.Count > 0;

        foreach (var remove in _reagentsToRemove)
        {
            _solution.RemoveReagent(solutionEnt, remove, amount);
        }

        return alcoholRemoved;
    }
}
