using System.Numerics;
using Content.Shared.ParaDrop;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Spawners;

namespace Content.Client._RMC14.ParaDrop;

public sealed partial class ParaDropSystem : SharedParaDropSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ParaDropAnimationMessage>(OnParaDropMessage);
    }

    private void OnParaDropMessage(ParaDropAnimationMessage ev)
    {
        if (!TryGetEntity(ev.Entity, out var entity))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!TryComp<SpriteComponent>(entity, out var entSprite))
            return;

        var animationEnt = Spawn(null, coordinates);
        var sprite = AddComp<SpriteComponent>(animationEnt);

        sprite.NoRotation = true;
        var effectLayer = sprite.AddLayer(ev.ParachuteSprite);
        sprite.LayerSetOffset(effectLayer, ParachuteEffectOffset);

        var despawn = AddComp<TimedDespawnComponent>(animationEnt);
        despawn.Lifetime = ev.FallDuration;

        _animPlayer.Stop(animationEnt, "parachute-animation");
        _animPlayer.Play(animationEnt, ReturnFallAnimation(ev.FallDuration), "parachute-animation");

        _animPlayer.Stop(entity.Value, "dropping-animation");
        _animPlayer.Play(entity.Value, ReturnFallAnimation(ev.FallDuration), "dropping-animation");
    }

    public Animation ReturnFallAnimation(float fallDuration)
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
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 7), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), fallDuration),
                    },
                },
            },
        };
    }
}
