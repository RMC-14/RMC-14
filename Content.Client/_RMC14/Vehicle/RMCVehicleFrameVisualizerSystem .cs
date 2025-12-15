using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Vehicle;

public sealed class RMCVehicleFrameVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float ShowThreshold = 0.9f;
    private const float MinAlpha = 0.15f;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCHardpointIntegrityComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var integrity, out var sprite))
        {
            if (!_sprite.LayerMapTryGet((uid, sprite), "damaged_frame", out var layer, false))
                continue;

            var max = integrity.MaxIntegrity > 0f ? integrity.MaxIntegrity : 1f;
            var fraction = integrity.Integrity / max;

            if (fraction >= ShowThreshold)
            {
                _sprite.LayerSetVisible((uid, sprite), layer, false);
                continue;
            }

            var t = fraction / ShowThreshold;
            var alpha = MinAlpha + (1f - MinAlpha) * t;

            _sprite.LayerSetVisible((uid, sprite), layer, true);
            _sprite.LayerSetColor((uid, sprite), layer, sprite.Color.WithAlpha(alpha));
        }
    }
}
