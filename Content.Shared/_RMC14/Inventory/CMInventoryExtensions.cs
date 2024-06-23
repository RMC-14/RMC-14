using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared._RMC14.Inventory;

public static class CMInventoryExtensions
{
    public static bool TryGetFirst(EntityUid storageId, EntityUid itemId, out ItemStorageLocation location)
    {
        location = default;

        var entities = IoCManager.Resolve<IEntityManager>();
        var storageSystem = entities.System<SharedStorageSystem>();

        if (!entities.TryGetComponent(storageId, out StorageComponent? storage) ||
            !entities.TryGetComponent(itemId, out ItemComponent? item))
        {
            return false;
        }

        var storageBounding = storage.Grid.GetBoundingBox();

        for (var y = storageBounding.Bottom; y <= storageBounding.Top; y++)
        {
            for (var x = storageBounding.Left; x <= storageBounding.Right; x++)
            {
                location = new ItemStorageLocation(0, (x, y));
                if (storageSystem.ItemFitsInGridLocation(itemId, storageId, location))
                {
                    return true;
                }
            }
        }

        location = default;
        return false;
    }
}
