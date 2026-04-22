using Content.Shared.Storage;
using Content.Shared.Lock;
using Robust.Client.GameObjects;

namespace Content.Client.Lock.Visualizers;

public sealed class LockVisualizerSystem : VisualizerSystem<LockVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LockVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, LockVisuals.Locked, out _, args.Component))
            return;

        // Lock state for the entity.
        if (!AppearanceSystem.TryGetData<bool>(uid, LockVisuals.Locked, out var locked, args.Component))
            locked = true;

        var open = false;
        AppearanceSystem.TryGetData(uid, StorageVisuals.Open, out open, args.Component);

        // RMC start. Get the RSI from the mapped lock layer so this visualizer can detect whether that layer supports the unlocked state and switch the overlay correctly. Extra functionality: lock visuals now also work when the lock overlay uses a different RSI than the item's base sprite, instead of only supporting BaseRSI.
        SpriteSystem.TryGetLayer((uid, args.Sprite), LockVisualLayers.Lock, out var lockLayer, false);
        var lockLayerRsi = lockLayer?.ActualRsi;
        var unlockedStateExists = !string.IsNullOrEmpty(comp.StateUnlocked) &&
                                  lockLayerRsi?.TryGetState(comp.StateUnlocked, out _) == true;
        // RMC end

        SpriteSystem.LayerSetVisible((uid, args.Sprite), LockVisualLayers.Lock, !open && (locked || unlockedStateExists));

        if (!open && unlockedStateExists)
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), LockVisualLayers.Lock, locked ? comp.StateLocked : comp.StateUnlocked);
    }
}

public enum LockVisualLayers : byte
{
    Lock
}
