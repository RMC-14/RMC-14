using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Fluids;

public sealed class RMCSpillableSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpillableComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SpillableComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!_combatMode.IsInCombatMode(args.User))
            return;

        if (_openable.IsClosed(ent.Owner))
            return;

        if (!_solutionContainer.TryGetSolution((ent.Owner, null), ent.Comp.SolutionName, out var soln, out var solution) ||
            solution.Volume <= FixedPoint2.Zero)
        {
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        var splashed = _solutionContainer.SplitSolution(soln.Value, solution.Volume);
        _puddle.TrySplashSpillAt(ent.Owner, args.ClickLocation, splashed, out _, user: args.User);
    }
}
