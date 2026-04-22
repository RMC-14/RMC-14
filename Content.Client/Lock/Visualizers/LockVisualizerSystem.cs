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

        // RMC start. RMC secure satchels keep their lock overlay states in the bag RSI, so this block hides the lock layer while the storage is open, swaps between locked/unlocked when that overlay exists, and otherwise falls back to showing the layer only while the entity is actually locked.
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
