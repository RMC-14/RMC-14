using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Blood;

public sealed class XenoPlasmaBloodSystem : EntitySystem
{
    [Dependency] private readonly XenoEggPlasmaSystem _eggPlasma = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public bool TryDrawPlasmaBlood(Entity<BloodstreamComponent> target, FixedPoint2 amount, [NotNullWhen(true)] out Solution? drawn)
    {
        drawn = null;
        if (!TryComp(target, out XenoPlasmaBloodComponent? plasmaBlood))
            return false;

        drawn = new Solution();
        if (!_solution.ResolveSolution(target.Owner, target.Comp.BloodSolutionName, ref target.Comp.BloodSolution))
            return true;

        var bloodSolution = target.Comp.BloodSolution.Value;
        var drawAmount = FixedPoint2.Min(amount, bloodSolution.Comp.Solution.Volume);
        if (drawAmount <= FixedPoint2.Zero)
            return true;

        var perShare = drawAmount / (plasmaBlood.PlasmaTypes.Count + 1);
        _solution.RemoveReagent(bloodSolution, target.Comp.BloodReagent, drawAmount);

        drawn.AddReagent(target.Comp.BloodReagent, perShare);
        foreach (var plasma in plasmaBlood.PlasmaTypes)
        {
            drawn.AddReagent(_eggPlasma.CreatePlasma(plasma, target), perShare);
        }

        return true;
    }
}
