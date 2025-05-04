using Content.Shared._RMC14.Xenonids.ForTheHive;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.ForTheHive;

public sealed class XenoForTheHiveVisualizerSystem : VisualizerSystem<ForTheHiveComponent>
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForTheHiveComponent, ForTheHiveActivatedEvent>(OnForTheHiveAdded);
        SubscribeLocalEvent<ForTheHiveComponent, ForTheHiveCancelledEvent>(OnForTheHiveRemoved);
    }

    private void OnForTheHiveAdded(Entity<ForTheHiveComponent> xeno, ref ForTheHiveActivatedEvent args)
    {
        if (!TryComp<SpriteComponent>(xeno, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(ForTheHiveVisualLayers.Base, out var layer))
            return;

        if (xeno.Comp.ActiveSprite != null)
        {
            sprite.LayerSetAnimationTime(layer, 0);
            sprite.LayerSetRSI(layer, xeno.Comp.ActiveSprite);
        }
    }

    private void OnForTheHiveRemoved(Entity<ForTheHiveComponent> xeno, ref ForTheHiveCancelledEvent args)
    {
        if (!TryComp<SpriteComponent>(xeno, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(ForTheHiveVisualLayers.Base, out var layer))
            return;

        if (xeno.Comp.BaseSprite != null)
        {
            sprite.LayerSetAnimationTime(layer, 0); //Reset Frames
            sprite.LayerSetRSI(layer, xeno.Comp.BaseSprite);
        }
    }

    protected override void OnAppearanceChange(EntityUid xeno, ForTheHiveComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (!HasComp<ActiveForTheHiveComponent>(xeno))
            return;

        if (sprite == null || !AppearanceSystem.TryGetData<float>(xeno, ForTheHiveVisuals.Time, out var ratio, args.Component))
            return;

        if (!sprite.LayerMapTryGet(ForTheHiveVisualLayers.Base, out var layer))
            return;

        if (ratio >= 0)
            sprite.LayerSetAnimationTime(layer, (float)(component.AnimationTimeBase.TotalSeconds * ratio));

    }
}
