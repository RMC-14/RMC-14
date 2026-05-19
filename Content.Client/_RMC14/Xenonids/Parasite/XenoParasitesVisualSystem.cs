using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Parasite;

public sealed class XenoParasitesVisualSystem : VisualizerSystem<XenoParasiteThrowerComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoParasiteThrowerComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentShutdown(Entity<XenoParasiteThrowerComponent> entity, ref ComponentShutdown args)
    {
        TryComp<SpriteComponent>(entity, out var spriteComp);

        if (spriteComp == null)
        {
            return;
        }

        var sprite = (entity.Owner, spriteComp);

        foreach (var layer in Enum.GetValues<ParasiteOverlayLayers>())
        {
            if (_sprite.LayerMapTryGet(sprite, layer, out var index, false))
            {
                _sprite.LayerSetVisible(sprite, layer, false);
                _sprite.LayerSetRsiState(sprite, layer, null);
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoParasiteThrowerComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData(uid, ParasiteOverlayVisuals.States, out bool[] states))
            return;

        string layerState = "para_";

        if(AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out bool downed) && downed)
            layerState = "para_downed_";
        else if(AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out bool resting) && resting)
            layerState = "para_rest_";

        foreach(var layer in Enum.GetValues<ParasiteOverlayLayers>())
        {
            if (!sprite.LayerMapTryGet(layer, out var paraLayer))
                continue;

            sprite.LayerSetVisible(layer, states[(int)layer]);

            sprite.LayerSetState(layer, $"{layerState}{(int)layer}");
        }
    }
}
