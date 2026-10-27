using Content.Shared._RMC14.Chemistry.TuringDispenser;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Chemistry.TuringDispenser;

public sealed class RMCTuringDispenserVisualizerSystem : VisualizerSystem<RMCTuringDispenserComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RMCTuringDispenserComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, PowerDeviceVisuals.Powered, out bool powered, args.Component))
            powered = true;

        AppearanceSystem.TryGetData(uid, TuringDispenserVisuals.Status, out TuringDispenserStatus status, args.Component);

        string? state = status switch
        {
            TuringDispenserStatus.Running => "running",
            TuringDispenserStatus.Finished => "full",
            TuringDispenserStatus.Stuck => "stuck",
            _ => null,
        };

        var sprite = (uid, args.Sprite);
        if (state != null)
            SpriteSystem.LayerSetRsiState(sprite, TuringDispenserVisualLayers.Status, state);

        SpriteSystem.LayerSetVisible(sprite, TuringDispenserVisualLayers.Status, powered && state != null);
    }
}
