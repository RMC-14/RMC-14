using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Buckle;
using Content.Shared.Mobs;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Emplacements;

public abstract partial class RMCSharedWeaponControllerSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponControllerComponent, BeforeAttemptShootEvent>(OnAdjustShotOrigin);
        SubscribeLocalEvent<WeaponControllerComponent, DismountActionEvent>(OnDismountAction);
        SubscribeLocalEvent<WeaponControllerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WeaponControllerComponent, KnockedDownEvent>(OnKnockedDown);
        SubscribeLocalEvent<WeaponControllerComponent, ToggleInvisibilityAttemptEvent>(OnToggleInvisibilityAttempt);
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

    private void OnMobStateChanged(Entity<WeaponControllerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        _buckle.Unbuckle(ent.Owner, ent);
    }

    private void OnKnockedDown(Entity<WeaponControllerComponent> ent, ref KnockedDownEvent args)
    {
        _buckle.Unbuckle(ent.Owner, ent);
    }

    private void OnToggleInvisibilityAttempt(Entity<WeaponControllerComponent> ent, ref ToggleInvisibilityAttemptEvent args)
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
    public bool TryGetControlledWeapon(EntityUid user, [NotNullWhen(true)] out EntityUid? controlledWeapon, [NotNullWhen(true)] out GunComponent? gunComp)
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

    /// <summary>
    ///     Allows the given entity to shoot the given weapon remotely.
    /// </summary>
    /// <param name="controller">The entity controlling the weapon</param>
    /// <param name="weapon">The weapon being controlled</param>
    public void StartControllingWeapon(EntityUid controller, EntityUid weapon)
    {
        var weaponController = EnsureComp<WeaponControllerComponent>(controller);
        weaponController.ControlledWeapon = GetNetEntity(weapon);
        Dirty(controller, weaponController);
    }
}
