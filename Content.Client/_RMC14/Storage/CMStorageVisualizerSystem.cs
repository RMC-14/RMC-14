using Content.Shared._CM14.Storage;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client._CM14.Storage;

public sealed class CMStorageVisualizerSystem : VisualizerSystem<CMStorageVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        CMStorageVisualizerComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // If empty
        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.StorageUsed, out var level, args.Component))
            return;

        if (level == 0)
        {
            args.Sprite.LayerSetVisible(component.StorageOpen, false);
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
            args.Sprite.LayerSetVisible(component.StorageOpen, true);
            args.Sprite.LayerSetVisible(component.StorageClosed, false);
        }
        else
        {
            args.Sprite.LayerSetVisible(component.StorageOpen, false);
            args.Sprite.LayerSetVisible(component.StorageClosed, true);
        }
    }
}
