using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._RMC14.Food;

public abstract class SharedRMCFoodSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance;
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOpenChangeFillLevelsComponent, OpenableOpenedEvent>(OnChangeFillOpened);
        SubscribeLocalEvent<RMCOpenChangeFillLevelsComponent, OpenableClosedEvent>(OnChangeFillClosed);
    }

    private void OnChangeFillOpened(Entity<RMCOpenChangeFillLevelsComponent> ent, ref OpenableOpenedEvent args)
    {
        if (!TryComp<SolutionContainerVisualsComponent>(ent, out var visuals))
            return;

        visuals.MaxFillLevels = ent.Comp.FillLevelsOpen;
    }

    private void OnChangeFillClosed(Entity<RMCOpenChangeFillLevelsComponent> ent, ref OpenableClosedEvent args)
    {
        if (!TryComp<SolutionContainerVisualsComponent>(ent, out var visuals))
            return;

        visuals.MaxFillLevels = ent.Comp.FillLevelsClosed;
    }
}
