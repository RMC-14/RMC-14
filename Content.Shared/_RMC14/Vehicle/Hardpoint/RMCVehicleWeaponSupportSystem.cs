using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleWeaponSupportSystem : EntitySystem
{
    [Dependency] private readonly RMCVehicleTopologySystem _topology = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
        SubscribeLocalEvent<GunComponent, GetWeaponAccuracyEvent>(OnGetAccuracy);
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!_topology.TryGetVehicle(ent.Owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out RMCVehicleWeaponSupportModifierComponent? mods))
            return;

        args.FireRate *= mods.FireRateMultiplier;
    }

    private void OnGetAccuracy(Entity<GunComponent> ent, ref GetWeaponAccuracyEvent args)
    {
        if (!_topology.TryGetVehicle(ent.Owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out RMCVehicleWeaponSupportModifierComponent? mods))
            return;

        args.AccuracyMultiplier *= mods.AccuracyMultiplier;
    }
}
