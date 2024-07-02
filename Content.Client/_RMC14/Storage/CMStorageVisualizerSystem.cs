using Content.Shared._RMC14.Storage;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Storage;

/// <summary>
/// Sets the empty, open, and closed layer visibility for a storage item.
/// </summary>
public sealed class CMStorageVisualizerSystem : VisualizerSystem<CMStorageVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        CMStorageVisualizerComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // If empty
        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.StorageUsed, out var used, args.Component))
            return;

        if (used == 0)
        {
            if (component.StorageOpen != null)
                args.Sprite.LayerSetVisible(component.StorageOpen, false);
            if (component.StorageClosed != null)
                args.Sprite.LayerSetVisible(component.StorageClosed, false);
            return;
        }

        // Open or closed
        if (!AppearanceSystem.TryGetData<SharedBagState>(uid,
                SharedBagOpenVisuals.BagState,
                out var state,
                args.Component))
            return;

        if (state == SharedBagState.Open)
        {
            if (component.StorageOpen != null)
                args.Sprite.LayerSetVisible(component.StorageOpen, true);
            if (component.StorageClosed != null)
                args.Sprite.LayerSetVisible(component.StorageClosed, false);
        }
        else
        {
            if (component.StorageOpen != null)
                args.Sprite.LayerSetVisible(component.StorageOpen, false);
            if (component.StorageClosed != null)
                args.Sprite.LayerSetVisible(component.StorageClosed, true);
        }
    }
}
