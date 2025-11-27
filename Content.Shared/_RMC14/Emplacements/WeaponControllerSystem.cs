using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._RMC14.Emplacements;

public sealed class WeaponControllerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponControllerComponent, BeforeAttemptShootEvent>(OnAdjustShotOrigin);
    }

    private void OnAdjustShotOrigin(Entity<WeaponControllerComponent> ent, ref BeforeAttemptShootEvent args)
    {
        if (ent.Comp.ControlledWeapon == null)
            return;

        args.Origin = Transform(GetEntity(ent.Comp.ControlledWeapon.Value)).Coordinates;
        args.Handled = true;
    }

    /// <summary>
    ///     Tries to get EntityUid of the weapon the entity is currently controlling.
    /// </summary>
    /// <param name="user">The entity being checked to see if it's controlling a weapon</param>
    /// <param name="controlledWeapon">The weapon being controlled by the user through the <see cref="WeaponControllerComponent"/></param>
    /// <param name="gunComp">The <see cref="GunComponent"/> of the controlled weapon</param>
    /// <returns>True if a controlled weapon is found</returns>
    public bool TryGetControlledWeapon(EntityUid user, out EntityUid controlledWeapon, [NotNullWhen(true)] out GunComponent? gunComp)
    {
        gunComp = null;
        controlledWeapon = default;
        if (!TryComp(user, out WeaponControllerComponent? weaponController) || weaponController.ControlledWeapon == null)
            return false;

        controlledWeapon = GetEntity(weaponController.ControlledWeapon.Value);
        if (!TryComp(controlledWeapon, out GunComponent? gunComponent))
            return false;

        gunComp = gunComponent;
        return true;
    }
}
