using Content.Shared._RMC14.Storage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.EntityPreset;

public sealed class RMCRandomizedPresetSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCStorageSystem _rmcStorage = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public void ApplyPreset(EntityUid entity, IRMCRandomizedPreset preset)
    {
        if (_net.IsClient)
            return;

        if (preset.RandomStartingGear.Count > 0)
        {
            foreach (var (slot, itemList) in preset.RandomStartingGear)
            {
                var randomItem = _random.Pick(itemList);
                Equip(entity, randomItem, false, slotName: slot);
            }
        }

        if (preset.RandomOutfits.Count > 0)
        {
            var gear = _random.Pick(preset.RandomOutfits);
            foreach (var item in gear)
            {
                Equip(entity, item, tryInHand: preset.TryRandomOutfitsInhand);
            }
        }

        if (_random.Prob(preset.PrimaryWeaponChance) &&
            preset.PrimaryWeapons.Count > 0)
        {
            var gear = _random.Pick(preset.PrimaryWeapons);
            foreach (var item in gear)
            {
                Equip(entity, item, tryInHand: true);
            }
        }

        if (preset.RandomWeapon.Count > 0)
        {
            var gear = _random.Pick(preset.RandomWeapon);
            foreach (var item in gear)
            {
                Equip(entity, item, tryEquip: preset.TryEquipRandomWeapon);
            }
        }

        if (preset.RandomGear.Count > 0)
        {
            var gear = _random.Pick(preset.RandomGear);
            foreach (var item in gear)
            {
                Equip(entity, item, tryInHand: true, tryEquip: false);
            }
        }

        if (preset.RandomGearOther.Count > 0)
        {
            foreach (var other in preset.RandomGearOther)
            {
                if (other.Count == 0)
                    continue;

                var gear = _random.Pick(other);
                foreach (var item in gear)
                {
                    Equip(entity, item, tryEquip: preset.TryEquipRandomOtherGear);
                }
            }
        }

        var rareItemNumber = _random.Next(1, preset.RareItemCoefficient);
        if (preset.RareItems.Count == 0)
            return;

        foreach (var (item, chance) in preset.RareItems)
        {
            if (rareItemNumber < chance.Item1 || rareItemNumber > chance.Item2)
                continue;

            Equip(entity, item, tryInHand: true);
            break;
        }
    }

    private void Equip(EntityUid entity, EntProtoId toSpawn, bool tryStorage = true, bool tryInHand = false, bool tryEquip = true, string? slotName = null)
    {
        var coordinates = _transform.GetMoverCoordinates(entity);
        var spawn = Spawn(toSpawn, coordinates);

        if (tryEquip)
        {
            var slots = _inventory.GetSlotEnumerator(entity);
            while (slots.MoveNext(out var slot))
            {
                if (slotName != null && slot.ID != slotName)
                    continue;

                if (slot.ContainedEntity != null)
                    continue;

                if (_inventory.TryEquip(entity, spawn, slot.ID, true))
                    return;
            }
        }

        if (tryStorage && TryInsertItemInStorage(entity, spawn))
            return;

        if (tryInHand && _hands.TryPickupAnyHand(entity, spawn))
            return;

        Log.Warning($"Couldn't equip {ToPrettyString(spawn)} on {ToPrettyString(entity)}");
        QueueDel(spawn);
    }

    private bool TryInsertItemInStorage(EntityUid entity, EntityUid toInsert)
    {
        var slots = _inventory.GetSlotEnumerator(entity, SlotFlags.BACK);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } storageItem ||
                !TryComp(storageItem, out StorageComponent? storage))
            {
                continue;
            }

            if (!_rmcStorage.CanInsertStoreSkill(storageItem, toInsert, entity, out _))
                return false;

            if (_storage.Insert(storageItem, toInsert, out _, storageComp: storage, playSound: false))
                return true;
        }

        return false;
    }
}
