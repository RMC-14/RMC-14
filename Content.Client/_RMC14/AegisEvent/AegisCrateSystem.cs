using Content.Shared._RMC14.AegisCrate;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.AegisCrate;

public sealed class AegisCrateSystem : SharedAegisCrateSystem
{
    private const string AnimationKey = "AegisCrateOpenAnim";
    private Animation? _openingAnimation;

    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AegisCrateComponent, AegisCrateStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<AegisCrateComponent, AnimationCompletedEvent>(OnAegisAnimationFinished);

        _openingAnimation = new Animation
        {
            Length = OpeningSpeed,
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

    protected override void OnStartup(Entity<AegisCrateComponent> crate, ref ComponentStartup args)
    {
        base.OnStartup(crate, ref args);
        SetVisuals(crate);
    }

    private void SetVisuals(Entity<AegisCrateComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) || !_sprite.LayerMapTryGet((ent, sprite), AegisCrateVisualLayers.Base, out var layer, true))
            return;

        var state = ent.Comp.State;

        switch (state)
        {
            case AegisCrateState.Closed:
                _sprite.LayerSetRsiState((ent, sprite), layer, "aegis_crate");
                break;

            case AegisCrateState.Opening:
                _sprite.LayerSetRsiState((ent, sprite), layer, "aegis_crate_opening");               // Play animation

                if (!_animation.HasRunningAnimation(ent, AnimationKey))
                    _animation.Play(ent, _openingAnimation!, AnimationKey);
                break;

            case AegisCrateState.Open:
                _sprite.LayerSetRsiState((ent, sprite), layer, "aegis_crate_open");
                break;
        }
    }

    private void OnStateChanged(Entity<AegisCrateComponent> ent, ref AegisCrateStateChangedEvent args)
    {
        SetVisuals(ent);
    }

    private void OnAegisAnimationFinished(Entity<AegisCrateComponent> ent, ref AnimationCompletedEvent args)
    {
        if(TryComp<AnimationPlayerComponent>(ent, out var player))
            _animation.Stop(ent, player, AnimationKey);
    }
}
