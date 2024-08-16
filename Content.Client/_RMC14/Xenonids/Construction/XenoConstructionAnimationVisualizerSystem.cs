using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using static Robust.Client.Animations.AnimationTrackSpriteFlick;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class XenoConstructionAnimationVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string ResinAnimationKey = "resin_build";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<XenoConstructionAnimationStartEvent>(OnAnimateResinBuilding);
        SubscribeLocalEvent<XenoConstructFramesComponent, AnimationCompletedEvent>(OnAnimationFinished);
    }

    private void OnAnimationFinished(Entity<XenoConstructFramesComponent> ent, ref AnimationCompletedEvent args)
    {
        QueueDel(ent);
    }

    private void OnAnimateResinBuilding(XenoConstructionAnimationStartEvent ev)
    {
        if (!TryGetEntity(ev.Effect, out var eff) ||
        !TryGetEntity(ev.Xeno, out var entity) ||
        !TryComp(entity, out XenoConstructionComponent? comp) ||
        !TryComp(eff, out XenoConstructFramesComponent? frames))
        {
            return;
        }

        if (_animation.HasRunningAnimation(eff.Value, ResinAnimationKey))
            return;

        List<KeyFrame> keys = new();

        double frameCounter = (comp.BuildDelay.TotalSeconds / frames.Frames.Count) / 6;

        for (int i = 0; i < frames.Frames.Count; i++)
        {
            keys.Add(new KeyFrame(frames.Frames[i], (i + 1) * (float) frameCounter));
        }

        AnimationTrackSpriteFlick track = new() { LayerKey = XenoConstructionVisualLayers.Animation };
        track.KeyFrames.AddRange(keys);

        var build = new Animation
        {
            Length = comp.BuildDelay,
            AnimationTracks =
            {
                track,
            },
        };


        _animation.Play(eff.Value, build, ResinAnimationKey);
    }
}
