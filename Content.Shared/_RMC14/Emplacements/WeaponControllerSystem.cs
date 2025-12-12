using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Buckle;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Emplacements;

public sealed class WeaponControllerSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponControllerComponent, BeforeAttemptShootEvent>(OnAdjustShotOrigin);
        SubscribeLocalEvent<WeaponControllerComponent, DismountActionEvent>(OnDismountAction);
    }

    private void OnAdjustShotOrigin(Entity<WeaponControllerComponent> ent, ref BeforeAttemptShootEvent args)
    {
        if (ent.Comp.ControlledWeapon == null)
            return;

        _container.TryGetContainingContainer( GetEntity(ent.Comp.ControlledWeapon.Value), out var container);

        if (container == null)
            return;

        var mount = container.Owner;
        var rotation = _transform.GetWorldRotation(mount);
        var rotatedOffset = rotation.RotateVec(args.Offset);

        args.Origin = Transform(mount).Coordinates.Offset(rotatedOffset);
        args.Handled = true;
    }

    private void OnDismountAction(Entity<WeaponControllerComponent> ent, ref DismountActionEvent args)
    {
        _buckle.Unbuckle(ent.Owner, ent);
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
