using System.Numerics;
using Content.Shared._RMC14.Pushup;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;

namespace Content.Client._RMC14.Pushup;

public sealed class RMCPushupVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimationKey = "rmc-pushup";
    private readonly Dictionary<EntityUid, Vector2> _baseOffsets = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPushupComponent, AfterAutoHandleStateEvent>(OnStateChanged);
        SubscribeLocalEvent<RMCPushupComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<RMCPushupComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCPushupComponent, RMCPushupVisualsChangedEvent>(OnVisualsChanged);
    }

    private void OnStateChanged(Entity<RMCPushupComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnVisualsChanged(Entity<RMCPushupComponent> ent, ref RMCPushupVisualsChangedEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnAnimationCompleted(Entity<RMCPushupComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !args.Finished || !ent.Comp.Active || !ent.Comp.Routine)
            return;

        Play(ent);
    }

    private void OnShutdown(Entity<RMCPushupComponent> ent, ref ComponentShutdown args)
    {
        Stop(ent);
    }

    private void UpdateVisuals(Entity<RMCPushupComponent> ent)
    {
        if (!ent.Comp.Active)
        {
            Stop(ent);
            return;
        }

        if (_baseOffsets.ContainsKey(ent))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        _baseOffsets[ent] = sprite.Offset;
        Play(ent);
    }

    private void Play(Entity<RMCPushupComponent> ent)
    {
        if (!_baseOffsets.TryGetValue(ent, out var baseOffset))
            return;

        var pixels = ent.Comp.Form == RMCPushupForm.Knees
            ? ent.Comp.KneeOffsetPixels
            : ent.Comp.ProperOffsetPixels;
        var eyeRotation = _eye.CurrentEye?.Rotation ?? Angle.Zero;
        var downOffset = eyeRotation.RotateVec(new Vector2(0, -pixels / EyeManager.PixelsPerMeter));
        var halfDuration = (float) ent.Comp.Duration.TotalSeconds / 2f;

        var animation = new Animation
        {
            Length = ent.Comp.Duration,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(baseOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(baseOffset + downOffset, halfDuration),
                        new AnimationTrackProperty.KeyFrame(baseOffset, halfDuration),
                    },
                },
            },
        };

        _animation.Play(ent, animation, AnimationKey);
    }

    private void Stop(Entity<RMCPushupComponent> ent)
    {
        if (!_baseOffsets.Remove(ent, out var baseOffset))
            return;

        if (TryComp(ent, out AnimationPlayerComponent? player) &&
            _animation.HasRunningAnimation(ent.Owner, player, AnimationKey))
        {
            _animation.Stop(ent.Owner, player, AnimationKey);
        }

        if (TryComp(ent, out SpriteComponent? sprite))
            _sprite.SetOffset((ent, sprite), baseOffset);
    }
}
