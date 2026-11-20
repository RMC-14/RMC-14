using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Chemistry;

public sealed class RMCPillColorVisualizerSystem : VisualizerSystem<RMCPillColorVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RMCPillColorVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !AppearanceSystem.TryGetData(uid, RMCPillColorVisuals.Color, out Color color))
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), RMCPillColorVisuals.Layer, out var layer, false))
            return;

        SpriteSystem.LayerSetColor((uid, sprite), layer, color);
    }
}
