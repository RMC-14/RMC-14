using System.Numerics;
using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.ParaDrop;
using Content.Shared._RMC14.Sprite;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Client._RMC14.ParaDrop;

public sealed partial class ParaDropSystem : SharedParaDropSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private const string DroppingAnimationKey = "dropping-animation";
    private const string SkyFallingAnimationKey = "sky-falling-animation";

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AnimationPlayerSystem));

        SubscribeLocalEvent<SkyFallingComponent, AfterAutoHandleStateEvent>(OnSkyFallingState);
        SubscribeLocalEvent<SkyFallingComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<ParaDroppingComponent, ComponentRemove>(OnParaDroppingRemove);
    }

    private void OnSkyFallingState(Entity<SkyFallingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) ||
            TerminatingOrDeleted(ent))
        {
            return;
        }

        var player = EnsureComp<AnimationPlayerComponent>(ent);
        if (_animPlayer.HasRunningAnimation(player, SkyFallingAnimationKey))
            return;

        ent.Comp.OriginalScale = sprite.Scale;
        ent.Comp.OriginalSpriteOffset = sprite.Offset;

        if (ent.Comp.RemainingTime <= 0)
            return;

        var fallOffset = GetFallOffset(ent, sprite, -1f);
        _animPlayer.Play((ent, player), GetFallingDisappearingAnimation(ent.Comp.RemainingTime, ent.Comp.OriginalScale, ent.Comp.AnimationScale, ent.Comp.OriginalSpriteOffset, fallOffset), SkyFallingAnimationKey);
    }

    private void OnComponentRemove(Entity<SkyFallingComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) ||
            TerminatingOrDeleted(ent))
        {
            return;
        }

        if (TryComp(ent, out AnimationPlayerComponent? animation))
            _animPlayer.Stop((ent, animation), SkyFallingAnimationKey);

        var spriteEnt = (ent, sprite);
        _sprite.SetScale(spriteEnt, ent.Comp.OriginalScale);
        _sprite.SetOffset(spriteEnt, ent.Comp.OriginalSpriteOffset);
    }

    private void OnParaDroppingRemove(Entity<ParaDroppingComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out AnimationPlayerComponent? animation))
            return;

        _animPlayer.Stop((ent, animation), DroppingAnimationKey);

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        _sprite.SetOffset((ent, sprite), ent.Comp.OriginalSpriteOffset);
    }

    public Animation ReturnFallAnimation(float fallDuration, Vector2 fallOffset, Vector2 offset = new ())
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
                        new AnimationTrackProperty.KeyFrame(fallOffset + offset, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0) + offset, fallDuration),
                    },
                },
            },
        };
    }

    private Animation GetFallingDisappearingAnimation(float duration, Vector2 originalScale, Vector2 endScale, Vector2 originalOffset, Vector2 fallOffset)
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
                        new AnimationTrackProperty.KeyFrame(originalOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(originalOffset + fallOffset, duration),
                    },
                },
            }
        };
    }

    private void SpawnParachute(float fallDuration, EntityCoordinates coordinates, ParaDroppableComponent paraDroppable, float multiplier, Vector2 offset = new())
    {
        var animationEnt = Spawn(paraDroppable.ParachutePrototype, coordinates);
        if (TryComp(animationEnt, out SpriteComponent? sprite))
            _sprite.SetScale((animationEnt, sprite), sprite.Scale * paraDroppable.ParachuteScale);

        var despawn = EnsureComp<TimedDespawnComponent>(animationEnt);
        despawn.Lifetime = fallDuration;

        AddComp<RMCUpdateClientLocationComponent>(animationEnt);
        var paraDropping = EnsureComp<ParaDroppingComponent>(animationEnt);
        paraDropping.RemainingTime = fallDuration;

        var fallOffset = new Vector2(0f, paraDroppable.FallHeight * multiplier);
        _animPlayer.Play(animationEnt, ReturnFallAnimation(fallDuration, fallOffset, offset), DroppingAnimationKey);
    }

    public void PlayFallAnimation(EntityUid fallingUid, float fallDuration, float timeRemaining, float fallHeight, string animationKey, ParaDroppableComponent? paraDroppable = null)
    {
        var multiplier = timeRemaining / fallDuration;
        var adjustedDuration = fallDuration * multiplier;
        var adjustedHeight = fallHeight * multiplier;

        if (timeRemaining > 0 && multiplier is > 0 and < 1)
        {
            var offset = new Vector2();
            var fallOffset = new Vector2(0f, adjustedHeight);
            if (EntityManager.TryGetComponent(fallingUid, out SpriteComponent? sprite))
            {
                offset = sprite.Offset;
                fallOffset = GetFallOffset(fallingUid, sprite, adjustedHeight);
            }

            if (TryComp(fallingUid, out ParaDroppingComponent? paraDropping))
                paraDropping.OriginalSpriteOffset = offset;

            _animPlayer.Play(fallingUid, ReturnFallAnimation(adjustedDuration, fallOffset, offset), animationKey);
            if (paraDroppable != null)
                SpawnParachute(adjustedDuration, _transform.GetMoverCoordinates(fallingUid), paraDroppable, multiplier, offset);
        }
    }

    private Vector2 GetFallOffset(EntityUid uid, SpriteComponent sprite, float height)
    {
        var offset = new Vector2(0f, height);
        if (sprite.NoRotation)
            return offset;

        var rotation = _transform.GetWorldRotation(uid) + _eye.CurrentEye.Rotation;
        if (sprite.SnapCardinals)
            rotation -= rotation.RoundToCardinalAngle();

        return (-rotation).RotateVec(offset);
    }

    public override void FrameUpdate(float frameTime)
    {
        var skyFallingQuery = EntityQueryEnumerator<SkyFallingComponent, SpriteComponent>();
        while (skyFallingQuery.MoveNext(out var uid, out var skyFalling, out var sprite))
        {
            if (sprite.NoRotation)
                continue;

            var height = -(sprite.Offset - skyFalling.OriginalSpriteOffset).Length();
            var offset = GetFallOffset(uid, sprite, height);
            _sprite.SetOffset((uid, sprite), skyFalling.OriginalSpriteOffset + offset);
        }

        var paraDroppingQuery = EntityQueryEnumerator<ParaDroppableComponent, ParaDroppingComponent, SpriteComponent>();
        while (paraDroppingQuery.MoveNext(out var uid, out _, out var paraDropping, out var sprite))
        {
            if (sprite.NoRotation || HasComp<SkyFallingComponent>(uid))
                continue;

            var height = (sprite.Offset - paraDropping.OriginalSpriteOffset).Length();
            var offset = GetFallOffset(uid, sprite, height);
            _sprite.SetOffset((uid, sprite), paraDropping.OriginalSpriteOffset + offset);
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

                _rmcSprite.UpdateSpriteTree(uid);
            }
        }
    }
}
