using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Clothing;

public sealed class HelmetAccessoriesSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    private EntityQuery<StorageComponent> _storageQuery;
    private EntityQuery<HelmetAccessoryComponent> _accessoryQuery;

    public override void Initialize()
    {
        base.Initialize();

        _storageQuery = GetEntityQuery<StorageComponent>();
        _accessoryQuery = GetEntityQuery<HelmetAccessoryComponent>();

        SubscribeLocalEvent<HelmetAccessoryHolderComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<HelmetAccessoryHolderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<HelmetAccessoryHolderComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
    }

    private void OnEntInserted(Entity<HelmetAccessoryHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnEntRemoved(Entity<HelmetAccessoryHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnGetEquipmentVisuals(Entity<HelmetAccessoryHolderComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (_inventory.TryGetSlot(args.Equipee, args.Slot, out var slot) &&
            (slot.SlotFlags & ent.Comp.Slot) == 0)
        {
            return;
        }

        if (!_storageQuery.TryComp(ent.Owner, out var storage))
            return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (storage.Container == null)
            return;

        var index = 0;
        foreach (var item in storage.Container.ContainedEntities)
        {
            var layer = $"enum.{nameof(HelmetAccessoryLayers)}.{HelmetAccessoryLayers.Helmet}{index}_{Name(ent.Owner)}";

            if (!_accessoryQuery.TryComp(item, out var accessoryComp))
                continue;

            args.Layers.Add((layer, new PrototypeLayerData
            {
                RsiPath = accessoryComp.Rsi.RsiPath.ToString(),
                State = accessoryComp.Rsi.RsiState,
                Visible = true,
            }));

            index++;
        }
    }
}
