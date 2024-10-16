using Content.Shared._RMC14.Xenonids.Egg;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Egg;

public sealed class XenoEggVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string AnimationKey = "rmc_egg_destroying";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggComponent, ComponentStartup>(SetVisuals);
        SubscribeLocalEvent<XenoEggComponent, XenoEggStateChangedEvent>(SetVisuals);

        SubscribeLocalEvent<DestroyedXenoEggComponent, ComponentStartup>(OnStartup);
    }

    private void SetVisuals<T>(Entity<XenoEggComponent> ent, ref T args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var state = ent.Comp.State switch
        {
            XenoEggState.Item => ent.Comp.ItemState,
            XenoEggState.Growing => ent.Comp.GrowingState,
            XenoEggState.Grown => ent.Comp.GrownState,
            XenoEggState.Opened => ent.Comp.OpenedState,
            XenoEggState.Opening => ent.Comp.OpeningState,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(state))
            return;

        sprite.LayerSetState(XenoEggLayers.Base, state);
    }

    private void OnStartup(Entity<DestroyedXenoEggComponent> ent, ref ComponentStartup args)
    {
        if (_animation.HasRunningAnimation(ent, AnimationKey))
            return;

        _animation.Play(ent,
           new Animation
           {
               Length = ent.Comp.AnimationTime,
               AnimationTracks =
               {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = ent.Comp.Layer,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.AnimationState, 0f),
                        },
                    },
               },
           },
           AnimationKey);
    }
}
