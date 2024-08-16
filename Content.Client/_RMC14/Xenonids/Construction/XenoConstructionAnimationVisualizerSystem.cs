using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using static Robust.Client.Animations.AnimationTrackSpriteFlick;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class XenoConstructionAnimationVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string ResinAnimationKey = "resin_build";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<XenoConstructionAnimationStartEvent>(OnAnimateResinBuilding);
    }

    private void OnAnimateResinBuilding(XenoConstructionAnimationStartEvent ev)
    {
        if (!TryGetEntity(ev.Effect, out var eff) ||
        !TryGetEntity(ev.Xeno, out var entity) ||
        !TryComp(entity, out XenoConstructionComponent? comp) ||
        !TryComp(eff, out XenoConstructionAnimationComponent? timing))
        {
            return;
        }

        if (_animation.HasRunningAnimation(eff.Value, ResinAnimationKey))
            return;

        if (!TryComp<SpriteComponent>(eff, out var sprite))
            return;

        sprite.LayerMapTryGet(XenoConstructionVisualLayers.Animation, out var layer);
        var state = sprite.LayerGetState(layer);

        timing.AnimationTime = comp.BuildDelay;
        if(sprite.TryGetLayer(layer, out var aLayer) && aLayer.ActualState != null)
            timing.TotalFrames = aLayer.ActualState.DelayCount;

        var build = new Animation
        {
            Length = comp.BuildDelay,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = XenoConstructionVisualLayers.Animation,
                    KeyFrames = { new KeyFrame(state, 0) },
                },
            },
        };
        _animation.Play(eff.Value, build, ResinAnimationKey);
    }
    private void Animate(SpriteComponent sprite, object layerKey, XenoConstructionAnimationComponent comp)
    {
        if (!sprite.LayerExists(layerKey) ||
            sprite[layerKey] is not Layer layer ||
            layer.ActualState?.DelayCount is not { } delays)
        {
            return;
        }
        layer.SetAutoAnimated(false);
        layer.AnimationFrame = (int) ((comp.AnimationTime.TotalSeconds - layer.AnimationTimeLeft) * comp.TotalFrames);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var elevatorQuery = EntityQueryEnumerator<XenoConstructionAnimationComponent, SpriteComponent>();
        while (elevatorQuery.MoveNext(out var effect, out var sprite))
        {
            Animate(sprite, XenoConstructionVisualLayers.Animation, effect);
        }

    }
}
