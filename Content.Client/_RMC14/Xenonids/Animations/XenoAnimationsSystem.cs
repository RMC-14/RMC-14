using System.Numerics;
using Content.Shared._RMC14.Xenonids.Animation;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Animations;
using static Robust.Client.Animations.AnimationTrackProperty;

namespace Content.Client._RMC14.Xenonids.Animations;

public sealed class XenoAnimationsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayLungeAnimationEvent>(OnPlayLungeAnimation);
    }

    private void OnPlayLungeAnimation(PlayLungeAnimationEvent ev)
    {
        if (!TryGetEntity(ev.EntityUid, out var entity))
            return;

        if (!ev.Client && _player.LocalEntity == entity)
            return;

        var direction = ev.Direction;
        var animation = GetLungeAnimation(direction);
        _animation.Stop(entity.Value, MeleeLungeKey);
        _animation.Play(entity.Value, animation, MeleeLungeKey);
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
