using System.Numerics;
using Content.Shared._RMC14.Effects;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Spawners;

namespace Content.Client._RMC14.Effects;

public sealed class RMCSpriteOffsetAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private const string AnimationKey = "rmc-sprite-offset";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSpriteOffsetAnimationComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCSpriteOffsetAnimationComponent> ent, ref ComponentStartup args)
    {
        var eyeRot = _eye.CurrentEye?.Rotation ?? Angle.Zero;

        var length = ent.Comp.Length ?? 1.5f;
        if (ent.Comp.Length == null && TryComp<TimedDespawnComponent>(ent, out var despawn))
            length = despawn.Lifetime;

        if (length <= 0f)
            return;

        var track = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Offset),
            InterpolationMode = AnimationInterpolationMode.Linear,
        };

        var path = ent.Comp.PathOffsets;
        if (path is { Count: > 1 })
        {
            var segments = path.Count - 1;
            var segmentLength = length / segments;
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(Rotate(path[0], eyeRot), 0f));

            for (var i = 1; i < path.Count; i++)
            {
                track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(Rotate(path[i], eyeRot), segmentLength));
            }
        }
        else
        {
            var start = Rotate(ent.Comp.StartingOffset, eyeRot);
            var end = ent.Comp.EndingOffset is { } rawEnd ? Rotate(rawEnd, eyeRot) : -start;
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(start, 0f));
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(end, length));
        }

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                track,
            },
        };

        _animation.Play(ent.Owner, animation, AnimationKey);
    }

    private static Vector2 Rotate(Vector2 offset, Angle eyeRot)
    {
        return eyeRot.RotateVec(offset);
    }
}
