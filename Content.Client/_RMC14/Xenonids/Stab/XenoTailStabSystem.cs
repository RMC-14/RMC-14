using System.Numerics;
using Content.Client.Interactable;
using Content.Shared._RMC14.Xenonids.Stab;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using static Robust.Client.Animations.AnimationTrackProperty;

namespace Content.Client._RMC14.Xenonids.Stab;

public sealed class XenoTailStabSystem : SharedXenoTailStabSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private const string TailAnimationKey = "cm-xeno-tail";
    private const string TailFadeAnimationKey = "cm-xeno-tail-fade";

    private bool _showTailAttack;

    public override void Initialize()
    {
        base.Initialize();

#if DEBUG
        _console.RegisterCommand("toggleshowtailattack", ToggleShowTailAttack);
#endif
    }

    public override void Shutdown()
    {
        base.Shutdown();

#if DEBUG
        _console.UnregisterCommand("toggleshowtailattack");
#endif
    }

    private void ToggleShowTailAttack(IConsoleShell shell, string s, string[] strings)
    {
        _showTailAttack = !_showTailAttack;

        if (_showTailAttack)
        {
            var overlay = new TailStabOverlay();
            _overlays.AddOverlay(overlay);
            shell.WriteLine("Enabled showing tail attack hitboxes");
        }
        else
        {
            _overlays.RemoveOverlay<TailStabOverlay>();
            shell.WriteLine("Disabled showing tail attack hitboxes");
        }
    }

    protected override void DoLunge(Entity<XenoTailStabComponent, TransformComponent> user, Vector2 localPos, EntProtoId animationId)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var animationEnt = Spawn(animationId, user.Comp2.Coordinates);
        _transform.SetParent(animationEnt, user);

        var sprite = EnsureComp<SpriteComponent>(animationEnt);
        sprite.NoRotation = true;
        sprite.Rotation = localPos.ToWorldAngle();

        // lie by 20% so the player feels less bad about missing
        var distance = localPos.Length() * 0.80f;

        var origin = _transform.GetMapCoordinates(user);
        var unobstructedDistance = _interaction.UnobstructedDistance(origin, origin.Offset(localPos));
        if (distance > unobstructedDistance)
            distance = unobstructedDistance;

        var startOffset = sprite.Rotation.RotateVec(new Vector2(0, -distance / 5f));
        var endOffset = sprite.Rotation.RotateVec(new Vector2(0, -distance));

        const float length = 0.10f;

        var moveAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new KeyFrame(startOffset, 0),
                        new KeyFrame(endOffset, length),
                    }
                }
            }
        };

        _animation.Play(animationEnt, moveAnimation, TailAnimationKey);

        var fadeOutAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(0.15f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new KeyFrame(sprite.Color, 0.05f),
                        new KeyFrame(sprite.Color.WithAlpha(0), length)
                    }
                }
            }
        };

        _animation.Play(animationEnt, fadeOutAnimation, TailFadeAnimationKey);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_overlays.TryGetOverlay(out TailStabOverlay? overlay))
        {
            overlay.Last = LastTailAttack;
        }
    }

    private sealed class TailStabOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public Box2Rotated? Last;

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (Last == null)
                return;

            args.WorldHandle.DrawRect(Last.Value, Color.Red);
        }
    }
}
