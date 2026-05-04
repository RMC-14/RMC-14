using Content.Shared._RMC14.Xenonids.ForTheHive;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Xenonids.ForTheHive;

public sealed class XenoForTheHiveVisualizerSystem : VisualizerSystem<ForTheHiveComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
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

        if (!_sprite.LayerMapTryGet((xeno.Owner, sprite), ForTheHiveVisualLayers.Base, out var layer, false))
            return;

        if (xeno.Comp.ActiveSprite != null)
        {
            _sprite.LayerSetAnimationTime((xeno.Owner, sprite), layer, 0);
            _sprite.LayerSetRsi((xeno.Owner, sprite), layer, new ResPath(xeno.Comp.ActiveSprite));
        }
    }

    private void OnForTheHiveRemoved(Entity<ForTheHiveComponent> xeno, ref ForTheHiveCancelledEvent args)
    {
        if (!TryComp<SpriteComponent>(xeno, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((xeno.Owner, sprite), ForTheHiveVisualLayers.Base, out var layer, false))
            return;

        if (xeno.Comp.BaseSprite != null)
        {
            _sprite.LayerSetAnimationTime((xeno.Owner, sprite), layer, 0); //Reset Frames
            _sprite.LayerSetRsi((xeno.Owner, sprite), layer, new ResPath(xeno.Comp.BaseSprite));
        }
    }

    protected override void OnAppearanceChange(EntityUid xeno, ForTheHiveComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (!HasComp<ActiveForTheHiveComponent>(xeno))
            return;

        if (sprite == null || !AppearanceSystem.TryGetData<float>(xeno, ForTheHiveVisuals.Time, out var ratio, args.Component))
            return;

        if (!_sprite.LayerMapTryGet((xeno, sprite), ForTheHiveVisualLayers.Base, out var layer, false))
            return;

        if (ratio >= 0)
            _sprite.LayerSetAnimationTime((xeno, sprite), layer, (float)(component.AnimationTimeBase.TotalSeconds * ratio));

    }
}
