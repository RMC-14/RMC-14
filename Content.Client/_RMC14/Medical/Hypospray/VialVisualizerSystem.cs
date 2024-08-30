using Content.Shared._RMC14.Chemistry;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.Hypospray;

public sealed class VialVisualizerSystem : VisualizerSystem<VialVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VialVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, VialVisuals.Occupied, out var occupied, args.Component) && occupied)
        {
            args.Sprite.LayerSetState(HyposprayVisualLayers.Base, comp.VialState);
        }
        else
        {
            args.Sprite.LayerSetState(HyposprayVisualLayers.Base, comp.EmptyState);
        }
    }
}

enum HyposprayVisualLayers : byte
{
    Base,
}
