using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Fruit.Events;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fruit;

public sealed class XenoFruitPlanterVisualsSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoFruitPlanterVisualsComponent, MobStateChangedEvent>(OnVisualsMobStateChanged);
        SubscribeLocalEvent<XenoFruitPlanterVisualsComponent, XenoRestEvent>(OnVisualsRest);
        SubscribeLocalEvent<XenoFruitPlanterVisualsComponent, XenoFruitPlanterVisualsChangedEvent>(OnVisualsFruitChanged);
    }

    private void OnVisualsMobStateChanged(Entity<XenoFruitPlanterVisualsComponent> xeno, ref MobStateChangedEvent args)
    {
        _appearance.SetData(xeno, XenoFruitPlanterVisuals.Downed, args.NewMobState != MobState.Alive);
    }

    private void OnVisualsRest(Entity<XenoFruitPlanterVisualsComponent> xeno, ref XenoRestEvent args)
    {
        _appearance.SetData(xeno, XenoFruitPlanterVisuals.Resting, args.Resting);
    }

    private void OnVisualsFruitChanged(Entity<XenoFruitPlanterVisualsComponent> xeno, ref XenoFruitPlanterVisualsChangedEvent args)
    {
        //if (!_prototype.TryIndex(args.Choice, out var fruit))
        //    return;

        if (!args.Choice.TryGet(out XenoFruitComponent? comp, _prototype, _compFactory))
            return;

        if (comp.Color is not { } color)
            return;

        _appearance.SetData(xeno, XenoFruitPlanterVisuals.Color, color);
    }
}
