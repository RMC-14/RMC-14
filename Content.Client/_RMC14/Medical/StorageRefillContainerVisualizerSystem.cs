using Content.Shared._RMC14.Medical.Refill;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Medical;

public sealed class StorageRefillContainerVisualizerSystem : VisualizerSystem<RMCRefillSolutionFromContainerOnStoreComponent>
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly CMRefillableSolutionSystem _refillable = default!;
    protected override void OnAppearanceChange(EntityUid uid, RMCRefillSolutionFromContainerOnStoreComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<Color>(uid, SolutionContainerStoreVisuals.Color, out var color))
            return;

        if (!sprite.LayerMapTryGet(SolutionContainerStoreVisuals.Base, out var colorLayer))
            return;

        if (!_container.TryGetContainer(uid, component.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var contained) ||
            (!_solution.TryGetDrainableSolution(contained.Value, out var drainable, out var sol) && !_refillable.TryGetPressurizedSolution(contained.Value, out drainable, out sol)) ||
            sol.Volume == 0)
        {
            sprite.LayerSetVisible(colorLayer, false);
            return;
        }

        sprite.LayerSetVisible(colorLayer, true);
        sprite.LayerSetColor(colorLayer, color.WithAlpha(component.LayerOpacity));
    }
}
