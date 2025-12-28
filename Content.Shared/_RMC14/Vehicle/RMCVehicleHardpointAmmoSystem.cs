using System;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleHardpointAmmoSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleHardpointAmmoComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnAmmoShot(Entity<RMCVehicleHardpointAmmoComponent> ent, ref AmmoShotEvent args)
    {
        if (!TryComp(ent.Owner, out BallisticAmmoProviderComponent? ammo))
            return;

        if (ammo.Count > 0)
            return;

        TryChamberNextMagazine(ent, ammo);
    }

    public bool TryChamberNextMagazine(Entity<RMCVehicleHardpointAmmoComponent> ent, BallisticAmmoProviderComponent ammo)
    {
        if (ent.Comp.StoredMagazines <= 0)
            return false;

        var magazineSize = Math.Max(1, ent.Comp.MagazineSize);
        var chamberSize = Math.Min(magazineSize, ammo.Capacity);

        ent.Comp.StoredMagazines--;
        Dirty(ent);

        _gun.SetBallisticUnspawned((ent.Owner, ammo), chamberSize);
        return true;
    }
}
