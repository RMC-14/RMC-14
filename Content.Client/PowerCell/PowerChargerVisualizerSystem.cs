using Content.Shared.Power;
using Content.Shared.PowerCell; // RMC14
using Robust.Client.GameObjects;

namespace Content.Client.PowerCell;

public sealed class PowerChargerVisualizerSystem : VisualizerSystem<PowerChargerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PowerChargerVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Update base item
        if (AppearanceSystem.TryGetData<bool>(uid, CellVisual.Occupied, out var occupied, args.Component) && occupied)
        {
            // TODO: don't throw if it doesn't have a full state
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Base, comp.OccupiedState);
        }
        else
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Base, comp.EmptyState);
        }

        // RMC14 - Update charge level indicator (priority over status lights)
        if (SpriteSystem.LayerExists((uid, args.Sprite), PowerChargerVisualLayers.Light))
        {
            if (!string.IsNullOrEmpty(comp.ChargeLevelState) &&
                AppearanceSystem.TryGetData<byte>(uid, PowerCellVisuals.ChargeLevel, out var chargeLevel, args.Component))
            {
                var chargeState = string.Format(comp.ChargeLevelState, chargeLevel);
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Light, chargeState);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerChargerVisualLayers.Light, true);
            }
            // RMC14 - Fallback to status lights if no charge level format is configured
            else if (AppearanceSystem.TryGetData<CellChargerStatus>(uid, CellVisual.Light, out var status, args.Component)
                && comp.LightStates.TryGetValue(status, out var lightState))
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Light, lightState);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerChargerVisualLayers.Light, true);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerChargerVisualLayers.Light, false);
            }
        }
    }
}

public enum PowerChargerVisualLayers : byte
{
    Base,
    Light,
}
