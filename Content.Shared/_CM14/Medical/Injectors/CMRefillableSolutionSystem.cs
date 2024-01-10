using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;

namespace Content.Shared._CM14.Medical.Injectors;

public sealed class CMRefillableSolutionSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMSolutionRefillerComponent, InteractUsingEvent>(OnRefillerInteractUsing);
    }

    private void OnRefillerInteractUsing(Entity<CMSolutionRefillerComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out CMRefillableSolutionComponent? refillable))
            return;

        if (!_solution.TryGetSolution(args.Used, refillable.Solution, out var solution))
            return;

        foreach (var (reagent, amount) in refillable.Reagents)
        {
            _solution.TryAddReagent(args.Used, solution, reagent, amount);
        }

        args.Handled = true;
    }
}
