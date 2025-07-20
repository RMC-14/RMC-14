using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class XenoConstructionAnimationVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<XenoConstructionAnimationStartEvent>(OnAnimateResinBuilding);
    }

    private void OnAnimateResinBuilding(XenoConstructionAnimationStartEvent ev)
    {
        if (!TryGetEntity(ev.Effect, out var eff) ||
        !TryGetEntity(ev.Xeno, out var entity) ||
        !TryComp(eff, out XenoConstructionAnimationComponent? timing))
        {
            return;
        }

        if (!TryComp<SpriteComponent>(eff, out var sprite))
            return;

        sprite.LayerMapTryGet(XenoConstructionVisualLayers.Animation, out var layer);
        var state = sprite.LayerGetState(layer);

        timing.AnimationTime = ev.BuildTime;
        timing.AnimationTimeFinished = _timing.CurTime + ev.BuildTime;
        if(sprite.TryGetLayer(layer, out var aLayer) && aLayer.ActualState != null)
            timing.TotalFrames = aLayer.ActualState.DelayCount;
    }

    private void Animate(SpriteComponent sprite, object layerKey, int frame)
    {
        if (!sprite.LayerExists(layerKey) ||
            sprite[layerKey] is not Layer layer)
        {
            return;
        }
        layer.SetAutoAnimated(layer.AnimationFrame < frame);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var constructQuery = EntityQueryEnumerator<XenoConstructionAnimationComponent, SpriteComponent>();
        while (constructQuery.MoveNext(out var uid, out var effect, out var sprite))
        {
            double progression = (effect.AnimationTimeFinished - _timing.CurTime) / effect.AnimationTime;
            if (progression < 0)
                progression = 0;
            int expectedFrame = (int) Math.Min(effect.TotalFrames * (1 - progression), effect.TotalFrames - 1);
            Animate(sprite, XenoConstructionVisualLayers.Animation, expectedFrame);
        }

    }
}
