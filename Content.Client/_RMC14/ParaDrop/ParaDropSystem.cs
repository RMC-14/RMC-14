using System.Numerics;
using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.Sprite;
using Content.Shared.ParaDrop;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Client._RMC14.ParaDrop;

public sealed partial class ParaDropSystem : SharedParaDropSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;

    private const string DroppingAnimationKey = "dropping-animation";
    private const string SkyFallingAnimationKey = "sky-falling-animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkyFallingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SkyFallingComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<ParaDroppingComponent, ComponentRemove>(OnParaDroppingRemove);
    }

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

    private Animation GetFallingDisappearingAnimation(float duration, Vector2 originalScale, Vector2 endScale)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(duration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(originalScale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(endScale, duration),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, -1), duration),
                    },
                },
            }
        };
    }

    private void OnComponentInit(Entity<SkyFallingComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) ||
            TerminatingOrDeleted(ent))
        {
            return;
        }

        ent.Comp.OriginalScale = sprite.Scale;

        if (!TryComp<AnimationPlayerComponent>(ent, out var player))
            return;

        if (_animPlayer.HasRunningAnimation(player, SkyFallingAnimationKey))
            return;

        _animPlayer.Play((ent, player), GetFallingDisappearingAnimation(ent.Comp.RemainingTime, ent.Comp.OriginalScale, ent.Comp.AnimationScale), SkyFallingAnimationKey);
    }

    private void OnComponentRemove(Entity<SkyFallingComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) ||
            TerminatingOrDeleted(ent))
        {
            return;
        }

        if (TryComp(ent, out AnimationPlayerComponent? animation))
            _animPlayer.Stop((ent, animation),SkyFallingAnimationKey);

        sprite.Scale = ent.Comp.OriginalScale;
    }

    private void OnParaDroppingRemove(Entity<ParaDroppingComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out AnimationPlayerComponent? animation))
            return;

        _animPlayer.Stop((ent, animation),DroppingAnimationKey);

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.Offset = new Vector2();
    }

    private void SpawnParachute(float fallDuration, EntityCoordinates coordinates, ParaDroppableComponent paraDroppable, float multiplier)
    {
        var animationEnt = Spawn(paraDroppable.ParachutePrototype, coordinates);
        var despawn = EnsureComp<TimedDespawnComponent>(animationEnt);
        despawn.Lifetime = fallDuration;

        AddComp<RMCUpdateClientLocationComponent>(animationEnt);
        var paraDropping = EnsureComp<ParaDroppingComponent>(animationEnt);
        paraDropping.RemainingTime = fallDuration;

        _animPlayer.Play(animationEnt, ReturnFallAnimation(fallDuration, paraDroppable.FallHeight * multiplier), DroppingAnimationKey);
    }

    public void PlayFallAnimation(EntityUid fallingUid, float fallDuration, float timeRemaining, float fallHeight, string animationKey, ParaDroppableComponent? paraDroppable = null)
    {
        var multiplier = timeRemaining / fallDuration;
        var adjustedDuration = fallDuration * multiplier;
        var adjustedHeight = fallHeight * multiplier;

        if (timeRemaining > 0 && multiplier is > 0 and < 1)
        {
            _animPlayer.Play(fallingUid, ReturnFallAnimation(adjustedDuration,  adjustedHeight), animationKey);
            if (paraDroppable != null)
                SpawnParachute(adjustedDuration, _transform.GetMoverCoordinates(fallingUid), paraDroppable, multiplier);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ParaDroppableComponent, ParaDroppingComponent>();
        while (query.MoveNext(out var uid, out var paraDroppable, out var paraDropping))
        {
            if (!HasComp<SkyFallingComponent>(uid))
            {
                if (!_animPlayer.HasRunningAnimation(uid, DroppingAnimationKey) && paraDroppable.LastParaDrop != null && Transform(uid).MapID != MapId.Nullspace)
                    PlayFallAnimation(uid, paraDroppable.DropDuration, paraDropping.RemainingTime, paraDroppable.FallHeight, DroppingAnimationKey, paraDroppable);

                _rmcSprite.UpdatePosition(uid);
            }
        }
    }
}
