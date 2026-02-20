using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Containers;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleWeaponSupportSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
        SubscribeLocalEvent<GunComponent, GetWeaponAccuracyEvent>(OnGetAccuracy);
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!TryGetVehicle(ent.Owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out RMCVehicleWeaponSupportModifierComponent? mods))
            return;

        args.FireRate *= mods.FireRateMultiplier;
    }

    private void OnGetAccuracy(Entity<GunComponent> ent, ref GetWeaponAccuracyEvent args)
    {
        if (!TryGetVehicle(ent.Owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out RMCVehicleWeaponSupportModifierComponent? mods))
            return;

        args.AccuracyMultiplier *= mods.AccuracyMultiplier;
    }

    private bool TryGetVehicle(EntityUid gun, out EntityUid vehicle)
    {
        vehicle = default;
        var current = gun;

        while (_containers.TryGetContainingContainer(current, out var container))
        {
            var containerOwner = container.Owner;
            if (HasComp<VehicleComponent>(containerOwner))
            {
                vehicle = containerOwner;
                return true;
            }

            current = containerOwner;
        }

        return false;
    }
}
