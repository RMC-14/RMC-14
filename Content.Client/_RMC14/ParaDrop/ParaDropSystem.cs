using System.Numerics;
using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.Sprite;
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
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;

    private const string ParachuteAnimationKey = "parachute-animation";
    private const string DroppingAnimationKey = "dropping-animation";

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

    public void PlayFallAnimation(EntityUid fallingUid, float fallDuration, TimeSpan fallStartTime, float fallHeight, string animationKey, ParaDroppableComponent? paraDroppable = null)
    {
        var duration = TimeSpan.FromSeconds(fallDuration);
        var timeRemaining =  duration - (_timing.CurTime - fallStartTime);
        var multiplier = (float) timeRemaining.Ticks / duration.Ticks;

        if (timeRemaining < TimeSpan.FromSeconds(fallDuration) && timeRemaining > TimeSpan.Zero &&
            multiplier is > 0 and < 1)
        {
            _animPlayer.Play(fallingUid, ReturnFallAnimation( multiplier * fallDuration,  fallHeight *  multiplier), animationKey);
            if (paraDroppable != null)
                SpawnParachute(multiplier * paraDroppable.DropDuration, _transform.GetMoverCoordinates(fallingUid), paraDroppable, multiplier);
        }
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

        AddComp<RMCUpdateClientLocationComponent>(animationEnt);

        _animPlayer.Play(animationEnt, ReturnFallAnimation(fallDuration, paraDroppable.FallHeight * multiplier), ParachuteAnimationKey);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ParaDroppableComponent, ParaDroppingComponent>();
        while (query.MoveNext(out var uid, out var paraDroppable, out _))
        {
            if (!_animPlayer.HasRunningAnimation(uid, DroppingAnimationKey) && paraDroppable.LastParaDrop != null)
                PlayFallAnimation(uid, paraDroppable.DropDuration, paraDroppable.LastParaDrop.Value, paraDroppable.FallHeight, DroppingAnimationKey, paraDroppable);

            // This is so the animation's current location gets updated during the drop.
            _rmcSprite.UpdatePosition(uid);
        }
    }
}
