using Content.Shared._RMC14.Doors;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using static Robust.Client.Animations.AnimationTrackSpriteFlick;

namespace Content.Client._RMC14.Doors;

public sealed class RMCDoorVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string ButtonAnimationKey = "rmc_pod_door_button_animation";
    private readonly TimeSpan _buttonAnimationLength = TimeSpan.FromSeconds(1.25);

    public override void Initialize()
    {
        SubscribeNetworkEvent<RMCPodDoorButtonPressedEvent>(OnPodDoorButtonPressed);

        SubscribeLocalEvent<RMCDoorButtonComponent, AnimationCompletedEvent>(OnDoorButtonAnimationCompleted);
    }

    private void OnPodDoorButtonPressed(RMCPodDoorButtonPressedEvent ev)
    {
        if (!TryGetEntity(ev.Button, out var entity) ||
            !TryComp(entity, out RMCDoorButtonComponent? comp))
        {
            return;
        }

        if (_animation.HasRunningAnimation(entity.Value, ButtonAnimationKey))
            return;

        var newState = ev.AnimationState;

        _animation.Play(entity.Value,
            new Animation
            {
                Length = _buttonAnimationLength,
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = RMCPodDoorButtonLayers.Animation,
                        KeyFrames =
                        {
                            new KeyFrame(newState, 0),
                            new KeyFrame(newState, 0.5f),
                            new KeyFrame(newState, 1f),
                            new KeyFrame(comp.OffState, 1.25f),
                        },
                    },
                },
            },
            ButtonAnimationKey);
    }

    private void OnDoorButtonAnimationCompleted(Entity<RMCDoorButtonComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != ButtonAnimationKey)
            return;

        if (TryComp(ent, out SpriteComponent? sprite))
            sprite.LayerSetState(RMCPodDoorButtonLayers.Animation, ent.Comp.OffState);
    }
}
