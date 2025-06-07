using Content.Shared._RMC14.SpecialBox;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using System;

namespace Content.Client._RMC14.SpecialBox;

public sealed class SpecialBoxSystem : EntitySystem
{
    private const string AnimationKey = "specialBoxOpenAnim";
    private Animation? _openingAnimation;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpecialBoxComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpecialBoxComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        // Add subscription for state changes
        SubscribeLocalEvent<SpecialBoxComponent, ComponentAdd>(OnComponentAdd);

        _openingAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(1.2),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = SpecialBoxVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame("special_box_opening", 0f),
                        new AnimationTrackSpriteFlick.KeyFrame("special_box_open", 1.2f)
                    }
                }
            }
        };
    }

    private void OnInit(EntityUid uid, SpecialBoxComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetState(SpecialBoxVisualLayers.Base, "special_box");
    }

    private void OnComponentAdd(EntityUid uid, SpecialBoxComponent component, ComponentAdd args)
    {
        // Watch for state changes
        component.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(EntityUid uid, SpecialBoxComponent comp)
    {
        if (comp.State == SpecialBoxState.Opening)
        {
            var animPlayer = EntityManager.System<AnimationPlayerSystem>();
            if (!animPlayer.HasRunningAnimation(uid, AnimationKey))
            {
                animPlayer.Play(uid, _openingAnimation!, AnimationKey);
            }
        }
    }

    private void OnAnimationCompleted(EntityUid uid, SpecialBoxComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        if (comp.State == SpecialBoxState.Opening)
        {
            comp.State = SpecialBoxState.Open;
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                sprite.LayerSetState(SpecialBoxVisualLayers.Base, "special_box_open");
            }
        }
    }
}
