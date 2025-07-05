using System.Numerics;
using Content.Shared.ParaDrop;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.ParaDrop;

public sealed partial class ParaDropSystem : SharedParaDropSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public Animation ReturnFallAnimation(float fallDuration, float fallHeight)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(fallDuration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, fallHeight), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0), fallDuration),
                    },
                },
            },
        };
    }

    private void SpawnParachute(float fallDuration, EntityCoordinates coordinates, ParaDroppableComponent paraDroppable, float multiplier)
    {
        var animationEnt = Spawn(null, coordinates);
        var sprite = AddComp<SpriteComponent>(animationEnt);

        sprite.NoRotation = true;
        var effectLayer = sprite.AddLayer(paraDroppable.ParachuteSprite);
        sprite.LayerSetOffset(effectLayer, paraDroppable.ParachuteOffset);

        var despawn = AddComp<TimedDespawnComponent>(animationEnt);
        despawn.Lifetime = fallDuration;

        _animPlayer.Play(animationEnt, ReturnFallAnimation(fallDuration, paraDroppable.FallHeight * multiplier), "parachute-animation");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ParaDroppableComponent, ParaDroppingComponent>();
        while (query.MoveNext(out var uid, out var paraDroppable, out _))
        {
            if (!_animPlayer.HasRunningAnimation(uid, "dropping-animation") && paraDroppable.LastParaDrop != null)
            {
                var duration = TimeSpan.FromSeconds(paraDroppable.DropDuration);
                var timeRemaining =  duration - (_timing.CurTime - paraDroppable.LastParaDrop.Value);
                var multiplier = (float) timeRemaining.Ticks / duration.Ticks;

                if (timeRemaining < TimeSpan.FromSeconds(paraDroppable.DropDuration) && timeRemaining > TimeSpan.Zero && multiplier is > 0 and < 1)
                {
                    SpawnParachute(multiplier * paraDroppable.DropDuration, _transform.GetMoverCoordinates(uid), paraDroppable, multiplier);
                    _animPlayer.Play(uid, ReturnFallAnimation( multiplier * paraDroppable.DropDuration,  paraDroppable.FallHeight *  multiplier), "dropping-animation");
                }
            }

            if (_timing.IsFirstTimePredicted)
                continue;

            // This is so the animation's current location gets updated during the drop.
            var oldPos = _transform.GetWorldPosition(uid);
            var newPos = oldPos with { Y = oldPos.Y + 0.0001f };
            _transform.SetWorldPosition(uid, newPos);
        }
    }
}
