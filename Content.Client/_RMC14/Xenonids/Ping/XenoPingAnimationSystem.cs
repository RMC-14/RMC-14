using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    private const float PulseDuration = 2.0f;
    private const float InitialScale = 1.4f;
    private const float PulseScale = 1.8f;
    private const string AnimationKey = "ping_pulse";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoPingEntityComponent, ComponentStartup>(OnPingStartup);
        SubscribeLocalEvent<XenoPingEntityComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnPingStartup(Entity<XenoPingEntityComponent> ent, ref ComponentStartup args)
    {
        BeginPingAnimation(ent.Owner);
    }

    private void OnAnimationCompleted(Entity<XenoPingEntityComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key == AnimationKey)
        {
            BeginPingAnimation(ent.Owner);
        }
    }

    private void BeginPingAnimation(EntityUid uid)
    {
        if (_animationPlayer.HasRunningAnimation(uid, AnimationKey))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var originalColor = sprite.Color;
        sprite.Scale = new Vector2(InitialScale);

        var inhaleTime = PulseDuration * 0.4f;
        var holdTime = PulseDuration * 0.2f;
        var exhaleTime = PulseDuration * 0.3f;
        var pauseTime = PulseDuration * 0.1f;

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(PulseDuration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(InitialScale), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(PulseScale), inhaleTime),
                        new AnimationTrackProperty.KeyFrame(new Vector2(PulseScale), holdTime),
                        new AnimationTrackProperty.KeyFrame(new Vector2(InitialScale), exhaleTime),
                        new AnimationTrackProperty.KeyFrame(new Vector2(InitialScale), pauseTime)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }
}
