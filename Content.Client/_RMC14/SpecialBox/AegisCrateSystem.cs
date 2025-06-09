using Content.Shared._RMC14.AegisCrate;
using Robust.Client.Animations;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using System;

namespace Content.Client._RMC14.AegisCrate;

public sealed class AegisCrateSystem : EntitySystem
{
    private const string AnimationKey = "AegisCrateOpenAnim";
    private Animation? _openingAnimation;

    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AegisCrateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AegisCrateComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        // Add subscription for state changes
        SubscribeLocalEvent<AegisCrateComponent, ComponentAdd>(OnComponentAdd);

        _openingAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(1.46), // 0.96 + 0.5 = 1.46s
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = AegisCrateVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame("aegis_crate_opening", 0f),
                        new AnimationTrackSpriteFlick.KeyFrame("aegis_crate_open", 1.46f)
                    }
                }
            }
        };
    }

    private void OnInit(EntityUid uid, AegisCrateComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetState(AegisCrateVisualLayers.Base, "aegis_crate");
    }

    private void OnComponentAdd(EntityUid uid, AegisCrateComponent component, ComponentAdd args)
    {
        // Watch for state changes
        component.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(EntityUid uid, AegisCrateComponent comp)
    {
        if (comp.State == AegisCrateState.Opening && comp.OpenSound != null)
        {
            _audio.PlayPvs(comp.OpenSound, uid, AudioParams.Default);
            var animPlayer = EntityManager.System<AnimationPlayerSystem>();
            if (!animPlayer.HasRunningAnimation(uid, AnimationKey))
            {
                animPlayer.Play(uid, _openingAnimation!, AnimationKey);
            }
        }

    }

    private void OnAnimationCompleted(EntityUid uid, AegisCrateComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        if (comp.State == AegisCrateState.Opening)
        {
            comp.State = AegisCrateState.Open;
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                sprite.LayerSetState(AegisCrateVisualLayers.Base, "aegis_crate_open");
            }
        }
    }
}
