using System;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Vehicle;

public sealed class RMCVehicleFrameDamageVisualizerSystem : VisualizerSystem<RMCHardpointIntegrityComponent>
{
    private const float ShowThreshold = 0.9f;
    private const float MinAlpha = 0.1f;

    protected override void OnAppearanceChange(EntityUid uid, RMCHardpointIntegrityComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;

        if (!sprite.LayerMapTryGet(RMCVehicleFrameDamageLayers.DamagedFrame, out var layer))
            return;

        float fraction;
        if (!AppearanceSystem.TryGetData(uid, RMCVehicleFrameDamageVisuals.IntegrityFraction, out fraction))
        {
            var max = component.MaxIntegrity > 0f ? component.MaxIntegrity : 1f;
            fraction = Math.Clamp(component.Integrity / max, 0f, 1f);
        }

        if (fraction >= ShowThreshold)
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        var t = fraction / ShowThreshold;
        var alpha = MinAlpha + (1f - MinAlpha) * (1f - t);

        sprite.LayerSetVisible(layer, true);
        sprite.LayerSetColor(layer, sprite.Color.WithAlpha(alpha));
    }
}
