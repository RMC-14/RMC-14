using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared._CM14.Storage;

public sealed class CMStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StorageFillComponent, CMStorageItemFillEvent>(OnStorageFillItem);
    }

    private void OnStorageFillItem(Entity<StorageFillComponent> storage, ref CMStorageItemFillEvent args)
    {
        if (!_storage.CanInsert(storage, args.Item, out var reason) &&
            reason == "comp-storage-insufficient-capacity")
        {
            // TODO CM14 make this error if this is a cm-specific storage
            Log.Warning($"Storage {ToPrettyString(storage)} can't fit {ToPrettyString(args.Item)}");

            var modified = false;
            foreach (var shape in _item.GetItemShape((args.Item, args.Item)))
            {
                var grid = args.Storage.Grid;
                if (grid.Count == 0)
                {
                    grid.Add(shape);
                    continue;
                }

                // TODO CM14 this might create more space than is necessary to fit the item if there is some free space left in the storage before expanding it
                var last = grid[^1];
                var expanded = new Box2i(last.Left, last.Bottom, last.Right + shape.Right + 1, last.Top);

                if (expanded.Top < shape.Top)
                    expanded.Top = shape.Top;

                grid[^1] = expanded;
                modified = true;
            }

            if (modified)
                Dirty(storage);
        }
    }

    public bool IgnoreItemSize(Entity<StorageComponent> storage, EntityUid item)
    {
        return TryComp(storage, out IgnoreContentsSizeComponent? ignore) &&
               ignore.Items.IsValid(item, EntityManager);
    }
}
