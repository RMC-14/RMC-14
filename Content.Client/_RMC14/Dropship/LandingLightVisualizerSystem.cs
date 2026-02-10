using Content.Shared._RMC14.Dropship;
using Robust.Client.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dropship;

public sealed class LandingLightVisualizerSystem : VisualizerSystem<LandingLightComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void OnAppearanceChange(EntityUid uid, LandingLightComponent light, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        SpriteSystem.LayerSetVisible((uid, args.Sprite), LandingLightVisuals.Off, !light.Enabled);
        SpriteSystem.LayerSetVisible((uid, args.Sprite), LandingLightVisuals.On, light.Enabled);
    }

    private void ApplyAnimation(EntityUid uid, SpriteComponent sprite, LandingLightComponent light)
    {
        var elapsed = (float)(_timing.CurTime - light.StartTime).TotalSeconds;

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), LandingLightVisuals.On, out var onLayerIndex, false))
            return;

        if (!SpriteSystem.TryGetLayer((uid, sprite), LandingLightVisuals.On, out var onLayer, false))
            return;

        if (onLayer.ActualRsi == null)
            return;

        var rsiState = SpriteSystem.GetState(new SpriteSpecifier.Rsi(onLayer.ActualRsi.Path, light.OnState));
        var time = elapsed % rsiState.AnimationLength;
        var delay = 0f;
        var frameIndex = 0;
        for (var i = 0; i < rsiState.DelayCount; i++)
        {
            delay += rsiState.GetDelay(i);
            if (!(time < delay))
                continue;

            frameIndex = i;
            break;
        }

        var texture = rsiState.GetFrames(RsiDirection.South)[frameIndex];
        SpriteSystem.LayerSetTexture((uid, sprite), onLayerIndex, texture);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LandingLightComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var light, out var sprite))
        {
            if (!light.Enabled)
                continue;

            ApplyAnimation(uid, sprite, light);
        }
    }
}
