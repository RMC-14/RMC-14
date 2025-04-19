using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Storage;
using Content.Shared.Storage;
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

        // If item has holster and the holster isn't empty,
        //  don't count holster's contents towards fill status
        if (TryComp(uid, out StorageComponent? storage) &&
            TryComp(uid, out CMHolsterComponent? holster) &&
            storage.Container.ContainedEntities.Count == holster.Contents.Count)
            used = 0;

        if (used == 0)
        {
            if (component.StorageOpen != null)
                args.Sprite.LayerSetVisible(component.StorageOpen, false);
            if (component.StorageClosed != null)
                args.Sprite.LayerSetVisible(component.StorageClosed, false);
            if (component.StorageEmpty != null)
                args.Sprite.LayerSetVisible(component.StorageEmpty, true);
            return;
        }
        else
        {
            if (component.StorageEmpty != null)
                args.Sprite.LayerSetVisible(component.StorageEmpty, false);
        }

        // Open or closed
        if (!AppearanceSystem.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        if (open)
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
