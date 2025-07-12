using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server._RMC14.Body;

public sealed class RMCBloodstreamSystem : SharedRMCBloodstreamSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override bool TryGetBloodSolution(EntityUid uid, [NotNullWhen(true)] out Solution? solution)
    {
        if (!TryComp(uid, out BloodstreamComponent? bloodstream))
        {
            solution = null;
            return false;
        }

        return _solution.TryGetSolution(uid, bloodstream.BloodSolutionName, out _, out solution);
    }

    public override bool TryGetChemicalSolution(EntityUid uid, out Entity<SolutionComponent> solutionEnt, [NotNullWhen(true)] out Solution? solution)
    {
        if (!TryComp(uid, out BloodstreamComponent? bloodstream) ||
            !_solution.TryGetSolution(uid, bloodstream.ChemicalSolutionName, out var nullableSolutionEnt, out solution))
        {
            solutionEnt = default;
            solution = null;
            return false;
        }

        solutionEnt = nullableSolutionEnt.Value;
        return true;
    }

    public override bool IsBleeding(EntityUid uid)
    {
        return CompOrNull<BloodstreamComponent>(uid) is { BleedAmount: > 0 };
    }
}
