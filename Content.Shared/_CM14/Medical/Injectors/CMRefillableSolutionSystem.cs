using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared._CM14.Medical.Injectors;

public sealed class CMRefillableSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMSolutionRefillerComponent, InteractUsingEvent>(OnRefillerInteractUsing);
    }

    private void OnRefillerInteractUsing(Entity<CMSolutionRefillerComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = true;
        if (!TryComp(args.Used, out CMRefillableSolutionComponent? refillable))
        {
            _popup.PopupClient($"The {Name(ent)} cannot refill the {Name(args.Used)}.", args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetSolution(args.Used, refillable.Solution, out var solution))
            return;


        foreach (var (reagent, amount) in refillable.Reagents)
        {
            _solution.TryAddReagent(solution.Value, reagent, amount);
        }

        _popup.PopupClient($"The {Name(ent)} makes a whirring noise as it refills your {Name(args.Used)}.", args.User, args.User);
    }
}
