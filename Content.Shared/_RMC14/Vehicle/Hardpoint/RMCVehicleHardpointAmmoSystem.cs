using System;
using System.Collections.Generic;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleHardpointAmmoSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleHardpointAmmoComponent, TakeAmmoEvent>(OnTakeAmmo, after: new[] { typeof(SharedGunSystem) });
        SubscribeLocalEvent<RMCVehicleHardpointAmmoComponent, OnEmptyGunShotEvent>(OnEmptyGunShot);
    }

    private void OnTakeAmmo(Entity<RMCVehicleHardpointAmmoComponent> ent, ref TakeAmmoEvent args)
    {
        if (!TryComp(ent.Owner, out BallisticAmmoProviderComponent? ammo))
            return;

        if (ammo.Count > 0)
            return;

        NormalizeAmmoQueue(ent, ammo);
    }

    private void OnEmptyGunShot(Entity<RMCVehicleHardpointAmmoComponent> ent, ref OnEmptyGunShotEvent args)
    {
        if (!TryComp(ent.Owner, out BallisticAmmoProviderComponent? ammo))
            return;

        if (ammo.Count > 0)
            return;

        NormalizeAmmoQueue(ent, ammo);
    }

    public bool NormalizeAmmoQueue(Entity<RMCVehicleHardpointAmmoComponent> ent, BallisticAmmoProviderComponent ammo)
    {
        if (ammo.Count > 0)
            return false;

        return TryChamberNextMagazine(ent, ammo);
    }

    public bool TryChamberNextMagazine(Entity<RMCVehicleHardpointAmmoComponent> ent, BallisticAmmoProviderComponent ammo)
    {
        var magazineSize = GetMagazineSize(ent.Comp, ammo);
        var slots = GetStoredRoundSlots(ent.Comp, magazineSize);
        var reserveSlot = -1;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i] <= 0)
                continue;

            reserveSlot = i;
            break;
        }

        if (reserveSlot < 0)
            return false;

        var chamberSize = Math.Min(magazineSize, slots[reserveSlot]);
        var updatedSlots = new int[slots.Count];
        for (var i = 0; i < slots.Count; i++)
            updatedSlots[i] = slots[i];

        updatedSlots[reserveSlot] -= chamberSize;
        CompactStoredRoundSlots(ent, updatedSlots, magazineSize);

        _gun.SetBallisticUnspawned((ent.Owner, ammo), chamberSize);
        return true;
    }

    public int GetMagazineSize(RMCVehicleHardpointAmmoComponent hardpointAmmo, BallisticAmmoProviderComponent ammo)
    {
        var magazineSize = Math.Max(1, hardpointAmmo.MagazineSize);
        var capacity = Math.Max(1, ammo.Capacity);
        return Math.Min(magazineSize, capacity);
    }

    public int GetStoredRounds(RMCVehicleHardpointAmmoComponent hardpointAmmo, int magazineSize)
    {
        if (hardpointAmmo.StoredRoundSlots.Count > 0)
        {
            var total = 0;
            var slots = Math.Min(hardpointAmmo.StoredRoundSlots.Count, GetMaxStoredRoundSlots(hardpointAmmo));
            for (var i = 0; i < slots; i++)
                total += Math.Clamp(hardpointAmmo.StoredRoundSlots[i], 0, Math.Max(1, magazineSize));

            return total;
        }

        return GetStoredRoundsFallback(hardpointAmmo, magazineSize);
    }

    public int GetMaxStoredRounds(RMCVehicleHardpointAmmoComponent hardpointAmmo, int magazineSize)
    {
        return GetMaxStoredRoundSlots(hardpointAmmo) * Math.Max(1, magazineSize);
    }

    public int GetMaxStoredRoundSlots(RMCVehicleHardpointAmmoComponent hardpointAmmo)
    {
        return Math.Max(0, hardpointAmmo.MaxStoredMagazines - 1);
    }

    public IReadOnlyList<int> GetStoredRoundSlots(RMCVehicleHardpointAmmoComponent hardpointAmmo, int magazineSize)
    {
        var maxSlots = GetMaxStoredRoundSlots(hardpointAmmo);
        var capacity = Math.Max(1, magazineSize);
        var slots = new int[maxSlots];

        if (hardpointAmmo.StoredRoundSlots.Count > 0)
        {
            var copy = Math.Min(maxSlots, hardpointAmmo.StoredRoundSlots.Count);
            for (var i = 0; i < copy; i++)
                slots[i] = Math.Clamp(hardpointAmmo.StoredRoundSlots[i], 0, capacity);

            return slots;
        }

        var remaining = Math.Clamp(GetStoredRoundsFallback(hardpointAmmo, magazineSize), 0, maxSlots * capacity);
        for (var i = 0; i < maxSlots && remaining > 0; i++)
        {
            slots[i] = Math.Min(capacity, remaining);
            remaining -= slots[i];
        }

        return slots;
    }

    public int GetStoredSlotRounds(RMCVehicleHardpointAmmoComponent hardpointAmmo, int reserveSlot, int magazineSize)
    {
        var slots = GetStoredRoundSlots(hardpointAmmo, magazineSize);
        return reserveSlot < 0 || reserveSlot >= slots.Count ? 0 : slots[reserveSlot];
    }

    public void SetStoredRounds(Entity<RMCVehicleHardpointAmmoComponent> ent, int rounds, int magazineSize)
    {
        var maxRounds = GetMaxStoredRounds(ent.Comp, magazineSize);
        var capacity = Math.Max(1, magazineSize);
        var remaining = Math.Clamp(rounds, 0, maxRounds);
        ent.Comp.StoredRoundSlots.Clear();

        for (var i = 0; i < GetMaxStoredRoundSlots(ent.Comp); i++)
        {
            var slotRounds = Math.Min(capacity, remaining);
            ent.Comp.StoredRoundSlots.Add(slotRounds);
            remaining -= slotRounds;
        }

        UpdateStoredRoundTotals(ent.Comp, capacity);
        Dirty(ent);
    }

    public void SetStoredSlotRounds(Entity<RMCVehicleHardpointAmmoComponent> ent, int reserveSlot, int rounds, int magazineSize)
    {
        var maxSlots = GetMaxStoredRoundSlots(ent.Comp);
        if (reserveSlot < 0 || reserveSlot >= maxSlots)
            return;

        var capacity = Math.Max(1, magazineSize);
        var slots = GetStoredRoundSlots(ent.Comp, capacity);

        ent.Comp.StoredRoundSlots.Clear();
        for (var i = 0; i < maxSlots; i++)
        {
            var slotRounds = i < slots.Count ? slots[i] : 0;
            if (i == reserveSlot)
                slotRounds = rounds;

            ent.Comp.StoredRoundSlots.Add(Math.Clamp(slotRounds, 0, capacity));
        }

        UpdateStoredRoundTotals(ent.Comp, capacity);
        Dirty(ent);
    }

    private static int GetStoredRoundsFallback(RMCVehicleHardpointAmmoComponent hardpointAmmo, int magazineSize)
    {
        if (hardpointAmmo.StoredRounds > 0)
            return hardpointAmmo.StoredRounds;

        return Math.Max(0, hardpointAmmo.StoredMagazines) * Math.Max(1, magazineSize);
    }

    private void CompactStoredRoundSlots(Entity<RMCVehicleHardpointAmmoComponent> ent, IReadOnlyList<int> slots, int magazineSize)
    {
        var maxSlots = GetMaxStoredRoundSlots(ent.Comp);
        var capacity = Math.Max(1, magazineSize);
        ent.Comp.StoredRoundSlots.Clear();

        foreach (var rounds in slots)
        {
            if (ent.Comp.StoredRoundSlots.Count >= maxSlots)
                break;

            var clamped = Math.Clamp(rounds, 0, capacity);
            if (clamped <= 0)
                continue;

            ent.Comp.StoredRoundSlots.Add(clamped);
        }

        while (ent.Comp.StoredRoundSlots.Count < maxSlots)
            ent.Comp.StoredRoundSlots.Add(0);

        UpdateStoredRoundTotals(ent.Comp, capacity);
        Dirty(ent);
    }

    private static void UpdateStoredRoundTotals(RMCVehicleHardpointAmmoComponent hardpointAmmo, int magazineSize)
    {
        var total = 0;
        foreach (var rounds in hardpointAmmo.StoredRoundSlots)
            total += Math.Clamp(rounds, 0, magazineSize);

        hardpointAmmo.StoredRounds = total;
        hardpointAmmo.StoredMagazines = total / Math.Max(1, magazineSize);
    }
}
