using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Station;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Loadout;

public sealed class LoadoutGearProfileSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadoutGearProfileComponent, StartingGearEquippedEvent>(OnStartingGearEquipped);
    }

    private void OnStartingGearEquipped(
        Entity<LoadoutGearProfileComponent> ent,
        ref StartingGearEquippedEvent args)
    {
        if (ent.Comp.Applied)
            return;

        ent.Comp.Applied = true;
        var preserved = new Dictionary<string, EntityUid>();
        foreach (var slot in ent.Comp.PreserveSlots)
        {
            if (_inventory.TryUnequip(ent.Owner,
                    slot,
                    out var item,
                    silent: true,
                    force: true,
                    reparent: false) &&
                item is { } preservedItem)
            {
                preserved[slot] = preservedItem;
            }
        }

        var storedItems = new List<EntityUid>();
        foreach (var slot in ent.Comp.ManagedSlots)
        {
            if (_inventory.TryUnequip(ent.Owner,
                    slot,
                    out var removed,
                    silent: true,
                    force: true,
                    reparent: false) &&
                removed is { } removedItem)
            {
                // Keep personal loadout items when the profile replaces their backpack.
                if (slot == "back" && TryComp(removedItem, out StorageComponent? storage))
                {
                    storedItems.AddRange(_container.EmptyContainer(storage.Container,
                        force: true,
                        destination: Transform(ent).Coordinates));
                }
                QueueDel(removedItem);
            }
        }

        _station.EquipStartingGear(ent.Owner, ent.Comp.StartingGear, raiseEvent: false);

        foreach (var (slot, item) in preserved)
        {
            if (!_inventory.TryEquip(ent.Owner, item, slot, silent: true, force: true))
                Log.Warning($"Failed to restore {ToPrettyString(item)} to {slot} after equipping loadout gear profile {ent.Comp.StartingGear}.");
        }

        foreach (var item in storedItems)
        {
            TryStashDisplacedItem(ent.Owner, item);
        }

        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private bool TryStashDisplacedItem(EntityUid owner, EntityUid item)
    {
        if (_inventory.TryGetSlots(owner, out var slots))
        {
            foreach (var slot in slots)
            {
                if (!_inventory.TryGetSlotEntity(owner, slot.Name, out var slotEnt) ||
                    !TryComp(slotEnt, out StorageComponent? storage))
                {
                    continue;
                }

                if (_storage.Insert(slotEnt.Value, item, out _, storageComp: storage, playSound: false))
                    return true;
            }
        }

        if (_hands.TryGetEmptyHand(owner, out var emptyHand) &&
            _hands.TryPickup(owner, item, emptyHand, checkActionBlocker: false))
        {
            return true;
        }

        return false;
    }
}
