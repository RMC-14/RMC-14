using Content.Shared._RMC14.AegisCrate;
using Robust.Client.Animations;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
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
        SubscribeLocalEvent<AegisCrateComponent, ComponentStartup>(SetVisuals);
        SubscribeLocalEvent<AegisCrateComponent, AfterAutoHandleStateEvent>(OnStateChanged);
        SubscribeLocalEvent<AegisCrateComponent, AnimationCompletedEvent>(OnAnimationCompleted);

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

    private void SetVisuals<T>(Entity<AegisCrateComponent> ent, ref T args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var state = ent.Comp.State;

        switch (state)
        {
            case AegisCrateState.Closed:
                sprite.LayerSetState(AegisCrateVisualLayers.Base, "aegis_crate");
                break;

            case AegisCrateState.Opening:
                sprite.LayerSetState(AegisCrateVisualLayers.Base, "aegis_crate_opening");                // Play opening sound and animation
                if (ent.Comp.OpenSound != null)
                    _audio.PlayPvs(ent.Comp.OpenSound, ent, AudioParams.Default);

                var animPlayer = EntityManager.System<AnimationPlayerSystem>();
                if (!animPlayer.HasRunningAnimation(ent, AnimationKey))
                    animPlayer.Play(ent, _openingAnimation!, AnimationKey);
                break;

            case AegisCrateState.Open:
                sprite.LayerSetState(AegisCrateVisualLayers.Base, "aegis_crate_open");
                break;
        }
    }

    private void OnStateChanged(Entity<AegisCrateComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        SetVisuals(ent, ref args);
    }

    private void OnAnimationCompleted(EntityUid uid, AegisCrateComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        // The server will handle state transitions, we just need to update final sprite state
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (comp.State == AegisCrateState.Open)
        {
            sprite.LayerSetState(AegisCrateVisualLayers.Base, "aegis_crate_open");
        }
    }
}
