using Content.Shared._RMC14.Animations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Animations;

public sealed class RMCAnimationSystem : SharedRMCAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string FlickId = "rmc_flick_animation";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RMCPlayAnimationEvent>(OnPlayAnimation);
        SubscribeNetworkEvent<RMCFlickEvent>(OnFlick);
    }

    private void OnPlayAnimation(RMCPlayAnimationEvent ev)
    {
        if (GetEntity(ev.Entity) is not { Valid: true } ent)
            return;

        if (_animation.HasRunningAnimation(ent, ev.Animation.Id))
            return;

        if (!TryComp(ent, out RMCAnimationComponent? animationComp) ||
            !animationComp.Animations.TryGetValue(ev.Animation, out var rmcAnimation))
        {
            return;
        }

        var animationTracks = new List<AnimationTrack>();
        foreach (var track in rmcAnimation.AnimationTracks)
        {
            var keyFrames = new List<AnimationTrackSpriteFlick.KeyFrame>();
            foreach (var keyFrame in track.KeyFrames)
            {
                keyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(keyFrame.State, keyFrame.KeyTime));
            }

            var spriteFlick = new AnimationTrackSpriteFlick { LayerKey = track.LayerKey };
            spriteFlick.KeyFrames.AddRange(keyFrames);
            animationTracks.Add(spriteFlick);
        }

        var animation = new Animation { Length = rmcAnimation.Length };
        animation.AnimationTracks.AddRange(animationTracks);
        _animation.Play(ent, animation, ev.Animation.Id);
    }

    private void OnFlick(RMCFlickEvent ev)
    {
        if (GetEntity(ev.Entity) is not { Valid: true } ent)
            return;

        if (_animation.HasRunningAnimation(ent, FlickId))
            _animation.Stop(ent, FlickId);

        var layer = ev.Layer ?? FlickId;
        if (!_sprite.LayerExists(ent, layer))
            _sprite.LayerMapSet(ent, FlickId, 0);

        var animationState = _sprite.GetState(ev.AnimationState);
        var length = animationState.AnimationLength;
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new RMCAnimationTrackSpriteFlick
                {
                    LayerKey = FlickId,
                    KeyFrames = new List<RMCAnimationTrackSpriteFlick.KeyFrame>
                    {
                        new(ev.AnimationState, 0),
                        new(ev.DefaultState, length),
                    },
                },
            },
        };

        _animation.Play(ent, animation, FlickId);
    }
}
