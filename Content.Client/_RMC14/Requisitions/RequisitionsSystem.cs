using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Requisitions;

public sealed class RequisitionsSystem : SharedRequisitionsSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimationKey = "cm_requisitions_animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequisitionsElevatorComponent, AfterAutoHandleStateEvent>(OnElevatorHandleState);
        SubscribeLocalEvent<RequisitionsGearComponent, AfterAutoHandleStateEvent>(OnGearHandleState);
        SubscribeLocalEvent<RequisitionsRailingComponent, AfterAutoHandleStateEvent>(OnRailingHandleState);
    }

    private void OnElevatorHandleState(Entity<RequisitionsElevatorComponent> elevator, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(elevator, out SpriteComponent? sprite) ||
            !_sprite.LayerMapTryGet((elevator.Owner, sprite), RequisitionsElevatorLayers.Base, out var layer, false))
        {
            return;
        }

        if (elevator.Comp.Mode != RequisitionsElevatorMode.Preparing)
            _animation.Stop(elevator.Owner, AnimationKey);

        switch (elevator.Comp.Mode)
        {
            case RequisitionsElevatorMode.Lowered:
                _sprite.LayerSetRsiState((elevator.Owner, sprite), layer, elevator.Comp.LoweredState);
                break;
            case RequisitionsElevatorMode.Raised:
                _sprite.LayerSetRsiState((elevator.Owner, sprite), layer, elevator.Comp.RaisedState);
                break;
            case RequisitionsElevatorMode.Lowering:
                elevator.Comp.LoweringAnimation ??= new Animation
                {
                    Length = TimeSpan.FromSeconds(2.1f),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = RequisitionsElevatorLayers.Base,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(elevator.Comp.LoweringState, 0)
                            }
                        }
                    }
                };

                _animation.Play(elevator, (Animation) elevator.Comp.LoweringAnimation, AnimationKey);
                break;
            case RequisitionsElevatorMode.Raising:
                elevator.Comp.RaisingAnimation ??= new Animation
                {
                    Length = TimeSpan.FromSeconds(2.1f),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = RequisitionsElevatorLayers.Base,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(elevator.Comp.RaisingState, 0)
                            }
                        }
                    }
                };

                _animation.Play(elevator, (Animation) elevator.Comp.RaisingAnimation, AnimationKey);
                break;
        }
    }

    private void OnGearHandleState(Entity<RequisitionsGearComponent> gear, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(gear, out SpriteComponent? sprite) ||
            !_sprite.LayerMapTryGet((gear.Owner, sprite), RequisitionsGearLayers.Base, out var layer, false))
        {
            return;
        }

        var state = gear.Comp.Mode switch
        {
            RequisitionsGearMode.Static => gear.Comp.StaticState,
            RequisitionsGearMode.Moving => gear.Comp.MovingState,
            _ => gear.Comp.StaticState
        };

        _sprite.LayerSetRsiState((gear.Owner, sprite), layer, state);
    }

    private void OnRailingHandleState(Entity<RequisitionsRailingComponent> railing, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(railing, out SpriteComponent? sprite) ||
            !_sprite.LayerMapTryGet((railing.Owner, sprite), RequisitionsRailingLayers.Base, out var layer, false))
        {
            return;
        }

        _animation.Stop(railing.Owner, AnimationKey);
        switch (railing.Comp.Mode)
        {
            case RequisitionsRailingMode.Lowered:
                _sprite.LayerSetRsiState((railing.Owner, sprite), layer, railing.Comp.LoweredState);
                break;
            case RequisitionsRailingMode.Raised:
                _sprite.LayerSetRsiState((railing.Owner, sprite), layer, railing.Comp.RaisedState);
                break;
            case RequisitionsRailingMode.Lowering:
                railing.Comp.LowerAnimation ??= new Animation
                {
                    Length = TimeSpan.FromSeconds(1.2f),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = RequisitionsRailingLayers.Base,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(railing.Comp.LoweringState, 0)
                            }
                        }
                    }
                };

                _animation.Play(railing, (Animation) railing.Comp.LowerAnimation, AnimationKey);
                break;
            case RequisitionsRailingMode.Raising:
                railing.Comp.RaiseAnimation ??= new Animation
                {
                    Length = TimeSpan.FromSeconds(1.2f),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = RequisitionsRailingLayers.Base,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(railing.Comp.RaisingState, 0)
                            }
                        }
                    }
                };

                _animation.Play(railing, (Animation) railing.Comp.RaiseAnimation, AnimationKey);
                break;
        }
    }

    private void Animate(EntityUid uid, SpriteComponent sprite, Enum layerKey)
    {
        if (!_sprite.LayerExists((uid, sprite), layerKey) ||
            !_sprite.TryGetLayer((uid, sprite), layerKey, out var layer, false) ||
            layer.ActualState?.DelayCount is not { } delays)
        {
            return;
        }

        if (layer.AnimationFrame >= delays - 1)
            _sprite.LayerSetAutoAnimated((uid, sprite), layerKey, false);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var elevatorQuery = EntityQueryEnumerator<RequisitionsElevatorComponent, SpriteComponent>();
        while (elevatorQuery.MoveNext(out var uid, out var elevator, out var sprite))
        {
            Animate(uid, sprite, elevator.Mode);
        }

        var railingQuery = EntityQueryEnumerator<RequisitionsRailingComponent, SpriteComponent>();
        while (railingQuery.MoveNext(out var uid, out var gear, out var sprite))
        {
            Animate(uid, sprite, gear.Mode);
        }
    }
}
