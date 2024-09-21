using Robust.Client.Animations;
using Content.Shared._RMC14.Sentry;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Sentry;

public sealed class SentryAnimationVisualizer : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string AnimationKey = "rmc_sentry_deploy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SentrySpikesComponent, AppearanceChangeEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<SentrySpikesComponent> spikes, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SentryComponent>(spikes, out var sentry) || sentry.Mode != SentryMode.On)
            return;

        if (_animation.HasRunningAnimation(spikes, AnimationKey))
            return;

        _animation.Play(spikes,
           new Animation
           {
               Length = spikes.Comp.AnimationTime,
               AnimationTracks =
               {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = spikes.Comp.Layer,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(spikes.Comp.AnimationState, 0f),
                        },
                    },
               },
           },
           AnimationKey);
    }
}
