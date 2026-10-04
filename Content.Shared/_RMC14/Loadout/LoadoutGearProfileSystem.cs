using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Station;

namespace Content.Shared._RMC14.Loadout;

public sealed class LoadoutGearProfileSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;

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
                QueueDel(removedItem);
            }
        }

        _station.EquipStartingGear(ent.Owner, ent.Comp.StartingGear, raiseEvent: false);

        foreach (var (slot, item) in preserved)
        {
            if (!_inventory.TryEquip(ent.Owner, item, slot, silent: true, force: true))
                Log.Warning($"Failed to restore {ToPrettyString(item)} to {slot} after equipping loadout gear profile {ent.Comp.StartingGear}.");
        }

        RemCompDeferred(ent.Owner, ent.Comp);
    }
}
