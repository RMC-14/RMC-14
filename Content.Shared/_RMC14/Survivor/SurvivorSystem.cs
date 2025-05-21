﻿using Content.Shared._RMC14.Armor;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Survivor;

public sealed class SurvivorSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipSurvivorPresetComponent, PlayerSpawnCompleteEvent>(OnPresetPlayerSpawnComplete, after: [typeof(CMArmorSystem)]);
    }

    private void OnPresetPlayerSpawnComplete(Entity<EquipSurvivorPresetComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        ApplyPreset(ent, ent.Comp.Preset);
    }

    private void ApplyPreset(EntityUid mob, EntProtoId<SurvivorPresetComponent> preset)
    {
        if (!preset.TryGet(out var comp, _prototypes, _compFactory))
            return;

        if (comp.RandomOutfits.Count > 0)
        {
            var gear = _random.Pick(comp.RandomOutfits);
            foreach (var item in gear)
            {
                Equip(mob, item, false);
            }
        }

        if (_random.Prob(comp.PrimaryWeaponChance) &&
            comp.PrimaryWeapons.Count > 0)
        {
            var gear = _random.Pick(comp.PrimaryWeapons);
            foreach (var item in gear)
            {
                Equip(mob, item, tryInHand: true);
            }
        }

        if (comp.RandomWeapon.Count > 0)
        {
            var gear = _random.Pick(comp.RandomWeapon);
            foreach (var item in gear)
            {
                Equip(mob, item);
            }
        }

        if (comp.RandomGear.Count > 0)
        {
            var gear = _random.Pick(comp.RandomGear);
            foreach (var item in gear)
            {
                Equip(mob, item);
            }
        }

        if (comp.RandomGearOther.Count > 0)
        {
            foreach (var other in comp.RandomGearOther)
            {
                if (other.Count == 0)
                    continue;

                var gear = _random.Pick(other);
                foreach (var item in gear)
                {
                    Equip(mob, item);
                }
            }
        }
    }

    private void Equip(EntityUid mob, EntProtoId toSpawn, bool tryStorage = true, bool tryInHand = false)
    {
        if (_net.IsClient)
            return;

        var coordinates = _transform.GetMoverCoordinates(mob);
        var spawn = Spawn(toSpawn, coordinates);
        var slots = _inventory.GetSlotEnumerator(mob);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity != null)
                continue;

            if (tryStorage)
            {
                var backs = _inventory.GetSlotEnumerator(mob, SlotFlags.BACK);
                while (backs.MoveNext(out var back))
                {
                    if (back.ContainedEntity is not { } backpack ||
                        !TryComp(backpack, out StorageComponent? storage))
                    {
                        continue;
                    }

                    if (_storage.Insert(backpack, spawn, out _, storageComp: storage))
                        return;
                }
            }

            if (_inventory.TryEquip(mob, spawn, slot.ID, true))
                return;
        }

        if (tryInHand && _hands.TryPickupAnyHand(mob, spawn))
            return;

        Log.Warning($"Couldn't equip {ToPrettyString(spawn)} on {ToPrettyString(mob)}");
        QueueDel(spawn);
    }
}
