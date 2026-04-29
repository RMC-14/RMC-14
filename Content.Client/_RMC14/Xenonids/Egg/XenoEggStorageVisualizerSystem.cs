using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Egg.EggRetriever;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Egg;

public sealed partial class XenoEggStorageVisualizerSystem : VisualizerSystem<XenoEggStorageVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggStorageVisualsComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentShutdown(Entity<XenoEggStorageVisualsComponent> entity, ref ComponentShutdown args)
    {
        TryComp<SpriteComponent>(entity, out var spriteComp);

        if (spriteComp == null)
        {
            return;
        }

        var sprite = (entity.Owner, spriteComp);

        if (_sprite.LayerMapTryGet(sprite, XenoEggStorageVisualLayers.Base, out var layer, false))
        {
            _sprite.LayerSetVisible(sprite, layer, false);
            _sprite.LayerSetRsiState(sprite, layer, null);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoEggStorageVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, XenoEggStorageVisuals.Number, out int eggs))
            return;

        if (!sprite.LayerMapTryGet(XenoEggStorageVisualLayers.Base, out var layer))
            return;

        string layerState = "eggsac_";

        int level = Math.Clamp((int)Math.Ceiling(((double)eggs / component.MaxEggs) * component.FullStates), 0, component.FullStates);
        layerState += level;

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
            layerState += "_downed";
        else if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
            layerState += "_rest";

        if (AppearanceSystem.TryGetData(uid, XenoEggStorageVisuals.Active, out bool active) && active)
            layerState += "_active";

        sprite.LayerSetState(layer, layerState);

        if (AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Dead, out bool dead) && dead)
            sprite.LayerSetVisible(layer, false);
        else
            sprite.LayerSetVisible(layer, true);
    }
}
