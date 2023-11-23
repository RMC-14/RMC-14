using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Headbutt;
using Content.Shared._CM14.Xenos.Hugger;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using static Robust.Shared.Utility.SpriteSpecifier;
using XenoLeapComponent = Content.Shared._CM14.Xenos.Hugger.XenoLeapComponent;

namespace Content.Client._CM14.Xenos.Hugger;

public sealed class XenoHuggerSystem : SharedXenoHuggerSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly XenoVisualizerSystem _xenoVisualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VictimHuggedComponent, AppearanceChangeEvent>(OnVictimHuggedAppearanceChanged);
        SubscribeLocalEvent<VictimBurstComponent, AppearanceChangeEvent>(OnVictimBurstAppearanceChanged);
    }

    private void OnVictimHuggedAppearanceChanged(Entity<VictimHuggedComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!_appearance.TryGetData(ent, ent.Comp.HuggedLayer, out bool hugged, args.Component))
            return;

        if (!sprite.LayerMapTryGet(ent.Comp.HuggedLayer, out var layer))
            layer = sprite.LayerMapReserveBlank(ent.Comp.HuggedLayer);

        if (hugged)
        {
            sprite.LayerSetSprite(layer, ent.Comp.HuggedSprite);
            sprite.LayerSetVisible(layer, true);
        }
        else
        {
            sprite.LayerSetVisible(layer, false);
        }
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
