using Content.Shared.Item;
using Content.Shared.Storage;

namespace Content.Shared._CM14.Storage;

[ByRefEvent]
public record struct CMStorageItemFillEvent(Entity<ItemComponent> Item, StorageComponent Storage);
