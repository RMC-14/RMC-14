using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Infected;

public sealed class XenoParasiteSystem : SharedXenoParasiteSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly XenoVisualizerSystem _xenoVisualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VictimBurstComponent, AppearanceChangeEvent>(OnVictimBurstAppearanceChanged);
        SubscribeLocalEvent<VictimInfectedComponent, AppearanceChangeEvent>(OnVictimInfectedAppearanceChanged);
    }

    private void OnVictimBurstAppearanceChanged(Entity<VictimBurstComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!_appearance.TryGetData(ent, ent.Comp.BurstLayer, out bool burst, args.Component))
            return;

        if (!sprite.LayerMapTryGet(ent.Comp.BurstLayer, out var layer))
            layer = sprite.LayerMapReserveBlank(ent.Comp.BurstLayer);

        if (burst)
        {
            sprite.LayerSetSprite(layer, ent.Comp.BurstSprite);
            sprite.LayerSetVisible(layer, true);
        }
        else
        {
            sprite.LayerSetVisible(layer, true);
        }
    }

    private void OnVictimInfectedAppearanceChanged(Entity<VictimInfectedComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!_appearance.TryGetData(ent, ent.Comp.BurstingLayer, out bool bursting, args.Component))
            return;

        if (!sprite.LayerMapTryGet(ent.Comp.BurstingLayer, out var layer))
            layer = sprite.LayerMapReserveBlank(ent.Comp.BurstingLayer);

        if (bursting)
        {
            sprite.LayerSetSprite(layer, ent.Comp.BurstingSprite);
            sprite.LayerSetVisible(layer, true);
        }
        else
        {
            sprite.LayerSetVisible(layer, false);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<XenoComponent, ThrownItemComponent, SpriteComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var thrown, out var sprite, out var appearance))
        {
            _xenoVisualizer.UpdateSprite((uid, sprite, null, appearance, null, thrown));
        }
    }
}
