using System;
using Content.Shared._RMC14.Vehicle.Supply;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._RMC14.Vehicle.Supply;

public sealed class RMCVehicleSupplyLiftSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string AnimationKey = "rmc_vehicle_supply_lift";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCVehicleSupplyLiftComponent, AfterAutoHandleStateEvent>(OnLiftHandleState);
    }

    private void OnLiftHandleState(Entity<RMCVehicleSupplyLiftComponent> lift, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(lift, out SpriteComponent? sprite) ||
            !sprite.LayerMapTryGet(RMCVehicleSupplyLiftLayers.Base, out var layer))
        {
            return;
        }

        if (lift.Comp.Mode != RMCVehicleSupplyLiftMode.Preparing)
            _animation.Stop(lift.Owner, AnimationKey);

        switch (lift.Comp.Mode)
        {
            case RMCVehicleSupplyLiftMode.Lowered:
                sprite.LayerSetState(layer, lift.Comp.LoweredState);
                break;
            case RMCVehicleSupplyLiftMode.Raised:
                sprite.LayerSetState(layer, lift.Comp.RaisedState);
                break;
            case RMCVehicleSupplyLiftMode.Lowering:
                lift.Comp.LoweringAnimation ??= new Animation
                {
                    Length = TimeSpan.FromSeconds(2.1f),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = RMCVehicleSupplyLiftLayers.Base,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(lift.Comp.LoweringState, 0)
                            }
                        }
                    }
                };

                _animation.Play(lift, (Animation) lift.Comp.LoweringAnimation, AnimationKey);
                break;
            case RMCVehicleSupplyLiftMode.Raising:
                lift.Comp.RaisingAnimation ??= new Animation
                {
                    Length = TimeSpan.FromSeconds(2.1f),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = RMCVehicleSupplyLiftLayers.Base,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(lift.Comp.RaisingState, 0)
                            }
                        }
                    }
                };

                _animation.Play(lift, (Animation) lift.Comp.RaisingAnimation, AnimationKey);
                break;
        }
    }
}
