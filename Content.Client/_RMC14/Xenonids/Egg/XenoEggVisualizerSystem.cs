using Content.Shared._RMC14.Xenonids.Egg;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._RMC14.Xenonids.Egg;

public sealed class XenoEggVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

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

        var expectedSprite = ent.Comp.CurrentSprite;

        if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / expectedSprite, out var res))
            return;

        if (sprite.BaseRSI != res.RSI)
        {
            sprite.BaseRSI = res.RSI;
        }

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

        if (sprite.LayerMapTryGet(XenoEggLayers.Base, out var layer))
            sprite.LayerSetState(layer, state);
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
