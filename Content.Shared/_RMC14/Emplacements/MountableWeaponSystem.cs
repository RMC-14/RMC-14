using Content.Shared._RMC14.Weapons.Ranged.Overheat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Emplacements;

public sealed class MountableWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedWeaponMountSystem _weaponMount = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MountableWeaponComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<MountableWeaponComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<MountableWeaponComponent, OverheatedEvent>(OnOverheated);
        SubscribeLocalEvent<MountableWeaponComponent, HeatGainedEvent>(OnHeatGained);
    }

    private void OnAttemptShoot(Entity<MountableWeaponComponent> ent, ref AttemptShootEvent args)
    {
        if (args.ToCoordinates == null)
            return;

        // Cancel the shot if the weapon is not mounted
        if (ent.Comp.RequiresMount && ent.Comp.MountedTo == null)
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.MountedTo == null)
            return;

        var mountEntity = GetEntity(ent.Comp.MountedTo.Value);

        // Cancel the shot if not aiming inside the cone of fire
        var mountPosition = _transform.GetWorldPosition(ent);
        var aimedLocation = _transform.ToWorldPosition(args.ToCoordinates.Value);

        var targetDirection = Angle.FromWorldVec(aimedLocation - mountPosition);
        var weaponFront = _transform.GetWorldRotation(mountEntity);
        var normalizedDirection = Angle.ShortestDistance(weaponFront, targetDirection).Degrees;

        if (Math.Abs(normalizedDirection) > ent.Comp.ShootArc / 2f)
        {
            args.Cancelled = true;

            // Rotate the mount to the aimed location if it's rotation is not locked
            if (TryComp(mountEntity, out WeaponMountComponent? mount) &&
                mount.CanRotateWithoutTool && args.ToCoordinates != null)
            {
                var diff = targetDirection.GetCardinalDir() - weaponFront.GetCardinalDir();

                if (diff > 4)
                    diff -= 8;
                else if (diff < -4)
                    diff += 8;

                _weaponMount.RotateMount((mountEntity, mount), args.User, diff * 45);
            }
            return;
        }

        // Cancel the shot if not enough free hands
        if (_hands.CountFreeHands(args.User) < ent.Comp.RequiredFreeHands)
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("mountable-weapon-no-free-hands"), args.User, PopupType.SmallCaution);
        }
    }

    private void OnTakeAmmo(Entity<MountableWeaponComponent> ent, ref TakeAmmoEvent args)
    {
        if (ent.Comp.MountedTo == null)
            return;

        if (!_slots.TryGetSlot(ent, "gun_magazine", out var itemSlot) ||  itemSlot.Item == null)
            return;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(itemSlot.Item.Value, ref ammoEv);

        var ammoSpriteKey = WeaponMountComponentVisualLayers.MountedAmmo;
        if (TryComp(ent, out FoldableComponent? foldableComp) && foldableComp.IsFolded)
            ammoSpriteKey = WeaponMountComponentVisualLayers.FoldedAmmo;

        _appearance.SetData(GetEntity(ent.Comp.MountedTo.Value), ammoSpriteKey, ammoEv.Count - 1 > 0);
    }

    private void OnOverheated(Entity<MountableWeaponComponent> ent, ref OverheatedEvent args)
    {
        if (ent.Comp.MountedTo == null || args.Damage == null)
            return;

        _weaponMount.OverheatMount(GetEntity(ent.Comp.MountedTo.Value), args.Damage);
    }

    private void OnHeatGained(Entity<MountableWeaponComponent> ent, ref HeatGainedEvent args)
    {
        if (ent.Comp.MountedTo == null)
            return;

        _weaponMount.UpdateAppearance(GetEntity(ent.Comp.MountedTo.Value));
    }
}
