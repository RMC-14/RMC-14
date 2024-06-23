using System.Numerics;
using Content.Shared._RMC14.Xenonids.Headbutt;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using static Robust.Client.Animations.AnimationTrackProperty;

namespace Content.Client._RMC14.Xenonids.Headbutt;

public sealed class XenoHeadbuttSystem : SharedXenoHeadbuttSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string MeleeLungeKey = "melee-lunge";

    protected override void DoLunge(EntityUid xeno, Vector2 direction)
    {
        var animation = GetLungeAnimation(direction);
        _animation.Stop(xeno, MeleeLungeKey);
        _animation.Play(xeno, animation, MeleeLungeKey);
    }

    private Animation GetLungeAnimation(Vector2 direction)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(0.4f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(Vector2.Zero, 0f),
                        new KeyFrame(direction.Normalized() * 0.6f, 0.1f),
                        new KeyFrame(Vector2.Zero, 0.3f)
                    }
                }
            }
        };
    }
}
