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

        // RMC start. RMC secure satchels store lock overlay states directly in their bag RSI. This block checks whether the current sprite supports an explicit unlocked overlay, hides the lock layer while the storage is being viewed open, and otherwise either: switches the overlay between locked/unlocked when that state exists, or 2) falls back to only showing the layer while the entity is actually locked.
        var unlockedStateExist = args.Sprite.BaseRSI?.TryGetState(comp.StateUnlocked, out _);
        if (AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), LockVisualLayers.Lock, !open);
        }
        else if (!(bool)unlockedStateExist!)
            SpriteSystem.LayerSetVisible((uid, args.Sprite), LockVisualLayers.Lock, locked);

        if (!open && (bool)unlockedStateExist!)
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), LockVisualLayers.Lock, locked ? comp.StateLocked : comp.StateUnlocked);
        // RMC end
    }
}

public enum LockVisualLayers : byte
{
    Lock
}
