using Content.Shared._RMC14.Shields;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Shields
{
    public sealed class XenoShieldVisualizerSystem : VisualizerSystem<XenoShieldComponent>
    {

        protected override void OnAppearanceChange(EntityUid uid, XenoShieldComponent component, ref AppearanceChangeEvent args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(RMCShieldVisuals.Base, out var layer))
                return;

            if (!AppearanceSystem.TryGetData<bool>(uid, RMCShieldVisuals.Active, out var active))
                return;

            if (!active)
            {
                sprite.LayerSetVisible(layer, false);
                return;
            }

            if (!AppearanceSystem.TryGetData<string>(uid, RMCShieldVisuals.Prefix, out var prefix) ||
                !AppearanceSystem.TryGetData<float>(uid, RMCShieldVisuals.Current, out var curr) ||
                !AppearanceSystem.TryGetData<float>(uid, RMCShieldVisuals.Max, out var max))
                return;

            var percent = curr / max;
            string state = prefix + "-";

            if (percent > 0.5)
                state += "full";
            else if (percent > 0.25)
                state += "half";
            else
                state += "quarter";

            sprite.LayerSetState(layer, state);
            sprite.LayerSetVisible(layer, true);
        }
    }
}
