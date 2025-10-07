using Content.Shared._RMC14.Xenonids.Hedgehog;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.Xenonids.Hedgehog;

public sealed class XenoSpikeShieldVisualizerSystem : VisualizerSystem<XenoSpikeShieldComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, XenoSpikeShieldComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, XenoSpikeShieldVisuals.Active, out bool active) && active)
        {
            // Add blue aura when shield is active
            sprite.Color = Color.FromHex("#4169E1"); // Royal blue tint
        }
        else
        {
            // Remove aura when shield is inactive
            sprite.Color = Color.White;
        }
    }
}