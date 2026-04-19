using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehiclePortGunSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly VehicleSystem _vehicleSystem = default!;
    [Dependency] private readonly VehicleViewToggleSystem _viewToggle = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehiclePortGunSeatComponent, StrapAttemptEvent>(OnPortGunSeatStrapAttempt);
        SubscribeLocalEvent<VehiclePortGunSeatComponent, UnstrappedEvent>(OnPortGunSeatUnstrapped);

        SubscribeLocalEvent<VehiclePortGunControllerComponent, InteractHandEvent>(OnPortGunInteractHand);
        SubscribeLocalEvent<VehiclePortGunControllerComponent, InteractUsingEvent>(OnPortGunInteractUsing);
        SubscribeLocalEvent<VehiclePortGunControllerComponent, ExaminedEvent>(OnPortGunExamined);
        SubscribeLocalEvent<VehiclePortGunControllerComponent, GetVerbsEvent<AlternativeVerb>>(OnPortGunVerbs);

        SubscribeLocalEvent<VehiclePortGunComponent, ComponentShutdown>(OnPortGunShutdown);
        SubscribeLocalEvent<VehiclePortGunComponent, GunShotEvent>(OnPortGunShot);
        SubscribeLocalEvent<VehiclePortGunComponent, EntInsertedIntoContainerMessage>(OnPortGunContainerInserted);
        SubscribeLocalEvent<VehiclePortGunComponent, EntRemovedFromContainerMessage>(OnPortGunContainerRemoved);
        SubscribeLocalEvent<VehiclePortGunOperatorComponent, ComponentShutdown>(OnPortGunOperatorShutdown);
    }

    private void OnPortGunSeatStrapAttempt(Entity<VehiclePortGunSeatComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_skills.HasSkills(args.Buckle.Owner, ent.Comp.Skills))
            return;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString("rmc-skills-cant-operate", ("target", ent)), args.Buckle, args.User);
    }

    private void OnPortGunSeatUnstrapped(Entity<VehiclePortGunSeatComponent> ent, ref UnstrappedEvent args)
    {
        if (_net.IsClient)
            return;

        ClearOperator(args.Buckle.Owner);
    }

    private void OnPortGunInteractHand(Entity<VehiclePortGunControllerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryGetPortGun(ent, args.User, out var vehicle, out var gunUid, out var portGun))
            return;

        if (portGun.Operator != null && portGun.Operator != args.User)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-in-use", ("operator", Name(portGun.Operator.Value))), ent, args.User);
            return;
        }

        if (TryComp(args.User, out VehiclePortGunOperatorComponent? existing) &&
            existing.Gun != null &&
            existing.Gun != gunUid)
        {
            ClearOperator(args.User, existing);
        }

        if (portGun.Operator == args.User)
        {
            ClearOperator(args.User);
            args.Handled = true;
            return;
        }

        portGun.Operator = args.User;
        Dirty(gunUid, portGun);

        var operatorComp = EnsureComp<VehiclePortGunOperatorComponent>(args.User);
        operatorComp.Gun = gunUid;
        operatorComp.Vehicle = vehicle;
        operatorComp.Controller = ent.Owner;
        Dirty(args.User, operatorComp);

        if (HasComp<VehicleEnterComponent>(vehicle))
        {
            _eye.SetTarget(args.User, vehicle);
            _viewToggle.EnableViewToggle(args.User, vehicle, ent.Owner, insideTarget: null, isOutside: true);
        }

        args.Handled = true;
    }

    private void OnPortGunInteractUsing(Entity<VehiclePortGunControllerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryGetGunFromController(ent, out var gunUid))
            return;

        if (!TryComp(gunUid, out ItemSlotsComponent? gunSlots) ||
            !_itemSlots.TryGetSlot(gunUid, "gun_magazine", out var magSlot, gunSlots))
        {
            return;
        }

        var ejected = false;
        if (magSlot.HasItem)
        {
            if (!_itemSlots.TryEjectToHands(gunUid, magSlot, args.User))
                return;

            ejected = true;
        }

        if (!_itemSlots.CanInsert(gunUid, args.Used, args.User, magSlot))
            return;

        if (!_hands.TryDrop(args.User, args.Used))
            return;

        if (_itemSlots.TryInsert(gunUid, magSlot, args.Used, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnPortGunShutdown(Entity<VehiclePortGunComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Operator == null)
            return;

        ClearOperator(ent.Comp.Operator.Value);
    }

    private void OnPortGunShot(Entity<VehiclePortGunComponent> ent, ref GunShotEvent args)
    {
        if (_net.IsClient)
            return;

    }

    private void OnPortGunContainerInserted(Entity<VehiclePortGunComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_net.IsClient)
            return;

    }

    private void OnPortGunContainerRemoved(Entity<VehiclePortGunComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_net.IsClient)
            return;

    }

    private void OnPortGunOperatorShutdown(Entity<VehiclePortGunOperatorComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Gun is not { } gun)
            return;

        if (!TryComp(gun, out VehiclePortGunComponent? portGun) || portGun.Operator != ent.Owner)
            return;

        portGun.Operator = null;
        Dirty(gun, portGun);
    }

    private void OnPortGunExamined(Entity<VehiclePortGunControllerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetGunFromController(ent, out var gunUid))
            return;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(gunUid, ref ammoEv);

        if (ammoEv.Capacity <= 0)
            return;

        args.PushMarkup(Loc.GetString("rmc-vehicle-portgun-examine-ammo", ("current", ammoEv.Count), ("max", ammoEv.Capacity)));
    }

    private void OnPortGunVerbs(Entity<VehiclePortGunControllerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!TryGetGunFromController(ent, out var gunUid))
            return;

        if (!TryComp(gunUid, out ItemSlotsComponent? gunSlots) ||
            !_itemSlots.TryGetSlot(gunUid, "gun_magazine", out var magSlot, gunSlots) ||
            !magSlot.HasItem)
        {
            return;
        }

        var user = args.User;
        var controller = ent.Owner;
        var gun = gunUid;
        var slot = magSlot;

        AlternativeVerb ejectVerb = new()
        {
            Text = Loc.GetString("rmc-vehicle-portgun-eject"),
            Act = () => _itemSlots.TryEjectToHands(gun, slot, user, excludeUserAudio: true),
            Priority = 2,
        };
        args.Verbs.Add(ejectVerb);
    }

    private bool TryGetPortGun(
        Entity<VehiclePortGunControllerComponent> ent,
        EntityUid user,
        out EntityUid vehicle,
        out EntityUid gunUid,
        out VehiclePortGunComponent portGun)
    {
        vehicle = default;
        gunUid = default;
        portGun = default!;

        if (!TryGetPortGunSeat(user))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-need-seat"), ent, user);
            return false;
        }

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-no-vehicle"), ent, user);
            return false;
        }

        vehicle = vehicleUid.Value;

        if (!TryComp(vehicle, out ItemSlotsComponent? itemSlots) ||
            !_itemSlots.TryGetSlot(vehicle, ent.Comp.GunSlotId, out var slot, itemSlots) ||
            !slot.HasItem ||
            slot.Item == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-no-gun"), ent, user);
            return false;
        }

        gunUid = slot.Item.Value;

        if (!TryComp(gunUid, out VehiclePortGunComponent? portGunComp) || !TryComp(gunUid, out GunComponent? _))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-no-gun"), ent, user);
            return false;
        }

        portGun = portGunComp;
        return true;
    }

    private bool TryGetPortGunSeat(EntityUid user)
    {
        if (!TryComp(user, out BuckleComponent? buckle) || buckle.BuckledTo == null)
            return false;

        return HasComp<VehiclePortGunSeatComponent>(buckle.BuckledTo.Value);
    }

    private bool TryGetGunFromController(Entity<VehiclePortGunControllerComponent> ent, out EntityUid gunUid)
    {
        gunUid = default;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicleUid) || vehicleUid == null)
            return false;

        var vehicle = vehicleUid.Value;

        if (!TryComp(vehicle, out ItemSlotsComponent? itemSlots) ||
            !_itemSlots.TryGetSlot(vehicle, ent.Comp.GunSlotId, out var slot, itemSlots) ||
            !slot.HasItem ||
            slot.Item == null)
        {
            return false;
        }

        gunUid = slot.Item.Value;
        return true;
    }

    private void ClearOperator(EntityUid user, VehiclePortGunOperatorComponent? operatorComp = null)
    {
        if (!Resolve(user, ref operatorComp, logMissing: false))
            return;

        if (operatorComp.Gun is { } gun &&
            TryComp(gun, out VehiclePortGunComponent? portGun) &&
            portGun.Operator == user)
        {
            portGun.Operator = null;
            Dirty(gun, portGun);
        }

        var vehicle = operatorComp.Vehicle;
        RemCompDeferred<VehiclePortGunOperatorComponent>(user);

        if (operatorComp.Controller != null)
            _viewToggle.DisableViewToggle(user, operatorComp.Controller.Value);

        if (_net.IsClient)
            return;

        if (vehicle != null && TryComp(user, out EyeComponent? eye) && eye.Target == vehicle)
            _eye.SetTarget(user, null, eye);
    }
}
