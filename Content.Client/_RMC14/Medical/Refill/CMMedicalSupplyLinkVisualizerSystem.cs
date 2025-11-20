using Content.Shared._RMC14.Medical.Refill;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.Refill;

public sealed class CMMedicalSupplyLinkVisualizerSystem : VisualizerSystem<CMMedicalSupplyLinkComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CMMedicalSupplyLinkComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, CMMedicalSupplyLinkVisuals.State, out var state, args.Component))
            return;

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, state);
    }
}
