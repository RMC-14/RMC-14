using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;


namespace Content.Shared._RMC14.Chemistry;

public abstract class RMCSharedHypospraySystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] protected readonly INetManager _net = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCHyposprayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCHyposprayComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<RMCHyposprayComponent, EntRemovedFromContainerMessage>(OnRemove);
    }

    private void OnStartup(Entity<RMCHyposprayComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnInsert(Entity<RMCHyposprayComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!ent.Comp.Initialized)
            return;

        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateAppearance(ent);
    }

    private void OnRemove(Entity<RMCHyposprayComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<RMCHyposprayComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;

        int containerEnts = container.ContainedEntities.Count;

        _appearance.SetData(ent, VialVisuals.Occupied, containerEnts != 0, appearance);

        if (!HasComp<SolutionContainerVisualsComponent>(ent))
            return;

        Solution? solution;
        if (containerEnts == 0)
            solution = new Solution();
        else
        {
            var vial = container.ContainedEntities[0];

            if (!_solution.TryGetSolution(vial, ent.Comp.VialName, out var soln))
                return;

            solution = soln.Value.Comp.Solution;
        }

        _appearance.SetData(ent, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearance);
        _appearance.SetData(ent, SolutionContainerVisuals.Color, solution.GetColor(_prototype), appearance);
        _appearance.SetData(ent, SolutionContainerVisuals.SolutionName, ent.Comp.SolutionName, appearance);

        if (solution.GetPrimaryReagentId() is { } reagent)
            _appearance.SetData(ent, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearance);

        Dirty(ent, ent.Comp);
    }
}
