using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Fruit.Events;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fruit;

public sealed class XenoFruitVisualsSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoFruitVisualsComponent, MobStateChangedEvent>(OnVisualsMobStateChanged);
        SubscribeLocalEvent<XenoFruitVisualsComponent, XenoRestEvent>(OnVisualsRest);
        SubscribeLocalEvent<XenoFruitVisualsComponent, XenoFruitVisualsChangedEvent>(OnVisualsFruitChanged);
    }

    private void OnVisualsMobStateChanged(Entity<XenoFruitVisualsComponent> xeno, ref MobStateChangedEvent args)
    {
        _appearance.SetData(xeno, XenoFruitVisuals.Downed, args.NewMobState != MobState.Alive);
    }

    private void OnVisualsRest(Entity<XenoFruitVisualsComponent> xeno, ref XenoRestEvent args)
    {
        _appearance.SetData(xeno, XenoFruitVisuals.Resting, args.Resting);
    }

    private void OnVisualsFruitChanged(Entity<XenoFruitVisualsComponent> xeno, ref XenoFruitVisualsChangedEvent args)
    {
        //if (!_prototype.TryIndex(args.Choice, out var fruit))
        //    return;

        if (!args.Choice.TryGet(out XenoFruitComponent? comp, _prototype, _compFactory))
            return;

        if (comp.Color is not { } color)
            return;

        _appearance.SetData(xeno, XenoFruitVisuals.Color, color);
    }
}
