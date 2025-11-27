using System.Numerics;
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
    public override void Initialize()
    {
        SubscribeLocalEvent<MountableWeaponComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<MountableWeaponComponent, TakeAmmoEvent>(OnTakeAmmo);
    }

    private void OnAttemptShoot(Entity<MountableWeaponComponent> ent, ref AttemptShootEvent args)
    {
        if (args.ToCoordinates == null)
            return;

        //Cancel the shot if the weapon is not mounted
        if (ent.Comp.RequiresMount && ent.Comp.MountedTo == null)
        {
            args.Cancelled = true;
            return;
        }

        // Cancel the shot if not aiming inside the cone of fire
        var arcDegrees = ent.Comp.ShootArc;
        var weaponFront = _transform.GetWorldRotation(ent).ToWorldVec();
        var aimedLocation = (_transform.ToWorldPosition(args.ToCoordinates.Value) - _transform.GetWorldPosition(ent)).Normalized();
        var dot = Math.Clamp(Vector2.Dot(weaponFront, aimedLocation), -1f, 1f);
        var angleDegrees = MathHelper.RadiansToDegrees(MathF.Acos(dot));

        if (angleDegrees >= arcDegrees / 2f)
        {
            args.Cancelled = true;
            return;
        }

        //Cancel the shot if not enough free hands
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
}
