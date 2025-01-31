using Content.Shared._RMC14.Xenonids.Parasite;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Infected;

public sealed class VictimBurstVisualizerSystem : VisualizerSystem<VictimBurstComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VictimBurstComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!AppearanceSystem.TryGetData(uid, BurstVisuals.Visuals, out VictimBurstState state, args.Component))
            return;

        if (args.Sprite == null)
            return;

        var rsiPath = component.RsiPath;

        var spriteState = state switch
        {
            VictimBurstState.Bursting => component.BurstingState,
            VictimBurstState.Burst => component.BurstState,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(spriteState))
            return;

        if (!args.Sprite.LayerMapTryGet(BurstLayer.Base, out var layer))
        {
            layer = args.Sprite.LayerMapReserveBlank(BurstLayer.Base);
            args.Sprite.LayerSetRSI(layer, rsiPath);
        }

        args.Sprite.LayerSetState(layer, spriteState);
    }
}
