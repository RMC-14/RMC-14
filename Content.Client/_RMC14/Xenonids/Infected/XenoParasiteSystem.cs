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

        SubscribeLocalEvent<VictimBurstComponent, ComponentStartup>(SetVisuals);
        SubscribeLocalEvent<VictimBurstComponent, VictimBurstStateChangedEvent>(SetVisuals);
    }

    private void SetVisuals<T>(Entity<VictimBurstComponent> ent, ref T args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var state = ent.Comp.State switch
        {
            BurstVisualState.Bursting => ent.Comp.BurstingState,
            BurstVisualState.Burst => ent.Comp.BurstState,
            _ => null
        };

        if (!sprite.LayerMapTryGet(ent.Comp.Layer, out var layer))
        {
            layer = sprite.LayerMapReserveBlank(ent.Comp.Layer);
            sprite.LayerSetRSI(layer, ent.Comp.BurstPath);
        }

        if (string.IsNullOrWhiteSpace(state))
            return;

        sprite.LayerSetState(layer, state);
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
