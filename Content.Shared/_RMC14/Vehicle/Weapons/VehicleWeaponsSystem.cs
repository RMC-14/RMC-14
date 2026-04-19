using System.Collections.Generic;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Movement.Systems;
using Content.Shared._RMC14.Weapons.Ranged;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleWeaponsSystem : EntitySystem
{
    private const string HardpointSelectActionId = "ActionVehicleSelectHardpoint";

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly VehicleTopologySystem _topology = default!;
    [Dependency] private readonly VehicleHardpointAmmoSystem _hardpointAmmo = default!;
    [Dependency] private readonly VehicleSystem _vehicleSystem = default!;
    [Dependency] private readonly VehicleTurretSystem _turretSystem = default!;
    [Dependency] private readonly VehicleViewToggleSystem _viewToggle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, StrapAttemptEvent>(OnWeaponSeatStrapAttempt);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, StrappedEvent>(OnWeaponSeatStrapped);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, UnstrappedEvent>(OnWeaponSeatUnstrapped);

        SubscribeLocalEvent<VehicleWeaponsSeatComponent, BoundUIOpenedEvent>(OnWeaponsUiOpened);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, BoundUIClosedEvent>(OnWeaponsUiClosed);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, VehicleWeaponsSelectMessage>(OnWeaponsSelect);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, VehicleWeaponsStabilizationMessage>(OnWeaponsStabilization);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, VehicleWeaponsAutoModeMessage>(OnWeaponsAutoMode);
        SubscribeLocalEvent<VehicleWeaponsOperatorComponent, ComponentShutdown>(OnOperatorShutdown);
        SubscribeLocalEvent<VehicleWeaponsOperatorComponent, ShotAttemptedEvent>(OnOperatorShotAttempted);
        SubscribeLocalEvent<VehicleWeaponsOperatorComponent, VehicleHardpointSelectActionEvent>(OnHardpointActionSelect);
        SubscribeLocalEvent<VehicleWeaponsOperatorComponent, VehicleViewToggledEvent>(OnViewToggled);

        SubscribeLocalEvent<HardpointSlotsChangedEvent>(OnHardpointSlotsChanged);

        SubscribeLocalEvent<VehicleTurretComponent, GunShotEvent>(OnTurretGunShot);
    }

    private void OnWeaponSeatStrapAttempt(Entity<VehicleWeaponsSeatComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_skills.HasSkills(args.Buckle.Owner, ent.Comp.Skills))
            return;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString("rmc-skills-cant-operate", ("target", ent)), args.Buckle, args.User);
    }

    private void OnWeaponSeatStrapped(Entity<VehicleWeaponsSeatComponent> ent, ref StrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
        {
            return;
        }

        var vehicleUid = vehicle.Value;
        var weapons = EnsureComp<VehicleWeaponsComponent>(vehicleUid);
        ClearOperatorSelections(weapons, args.Buckle.Owner);
        if (ent.Comp.IsPrimaryOperatorSeat)
        {
            weapons.Operator = args.Buckle.Owner;
        }
        RecalculateSelectedWeapon(vehicleUid, weapons);
        Dirty(vehicleUid, weapons);

        var operatorComp = EnsureComp<VehicleWeaponsOperatorComponent>(args.Buckle.Owner);
        operatorComp.Vehicle = vehicle;
        operatorComp.SelectedWeapon = null;
        operatorComp.HardpointActions.Clear();
        Dirty(args.Buckle.Owner, operatorComp);

        RefreshOperatorSelectedWeapons(vehicleUid, weapons);
        RefreshHardpointActions(args.Buckle.Owner, vehicleUid, weapons, operatorComp);

        if (HasComp<VehicleEnterComponent>(vehicleUid))
        {
            _eye.SetTarget(args.Buckle.Owner, vehicleUid);
            _viewToggle.EnableViewToggle(args.Buckle.Owner, vehicleUid, ent.Owner, insideTarget: null, isOutside: true);
        }

        UpdateGunnerView(args.Buckle.Owner, vehicleUid, ent.Comp);

        _ui.OpenUi(ent.Owner, VehicleWeaponsUiKey.Key, args.Buckle.Owner);
        UpdateWeaponsUiForAllOperators(vehicleUid, weapons);
    }

    private void OnWeaponSeatUnstrapped(Entity<VehicleWeaponsSeatComponent> ent, ref UnstrappedEvent args)
    {
        if (_net.IsClient)
            return;

        if (TryComp(args.Buckle.Owner, out VehicleWeaponsOperatorComponent? operatorComp))
            ClearHardpointActions(args.Buckle.Owner, operatorComp);

        RemCompDeferred<VehicleWeaponsOperatorComponent>(args.Buckle.Owner);
        _ui.CloseUi(ent.Owner, VehicleWeaponsUiKey.Key, args.Buckle.Owner);
        UpdateGunnerView(args.Buckle.Owner, ent.Owner, ent.Comp, removeOnly: true);

        _viewToggle.DisableViewToggle(args.Buckle.Owner, ent.Owner);

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle == null)
            return;

        var vehicleUid = vehicle.Value;
        if (TryComp(vehicleUid, out VehicleWeaponsComponent? weapons) &&
            ent.Comp.IsPrimaryOperatorSeat &&
            weapons.Operator == args.Buckle.Owner)
        {
            weapons.Operator = null;
            ClearOperatorSelections(weapons, args.Buckle.Owner);
            RecalculateSelectedWeapon(vehicleUid, weapons);
            Dirty(vehicleUid, weapons);
        }
        else if (TryComp(vehicleUid, out VehicleWeaponsComponent? otherWeapons))
        {
            ClearOperatorSelections(otherWeapons, args.Buckle.Owner);
            RecalculateSelectedWeapon(vehicleUid, otherWeapons);
            Dirty(vehicleUid, otherWeapons);
        }

        if (TryComp(vehicleUid, out VehicleWeaponsComponent? selectionWeapons))
            RefreshOperatorSelectedWeapons(vehicleUid, selectionWeapons);

        if (TryComp(vehicleUid, out VehicleWeaponsComponent? refreshedWeapons))
            UpdateWeaponsUiForAllOperators(vehicleUid, refreshedWeapons);

        if (TryComp(args.Buckle.Owner, out EyeComponent? eye) && eye.Target == vehicleUid)
            _eye.SetTarget(args.Buckle.Owner, null, eye);
    }

    private void OnOperatorShutdown(Entity<VehicleWeaponsOperatorComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        ClearHardpointActions(ent.Owner, ent.Comp);
    }

    private void OnOperatorShotAttempted(Entity<VehicleWeaponsOperatorComponent> ent, ref ShotAttemptedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.User != ent.Owner)
            return;

        if (ent.Comp.Vehicle is not { } vehicle)
            return;

        if (TryComp(vehicle, out HardpointIntegrityComponent? frameIntegrity) &&
            frameIntegrity.Integrity <= 0f)
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-hull-destroyed"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(vehicle, out VehicleWeaponsComponent? weapons) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots) ||
            !CanUseHardpointActions(ent.Owner) ||
            !weapons.OperatorSelections.TryGetValue(ent.Owner, out var selectedWeapon) ||
            selectedWeapon != args.Used.Owner)
        {
            return;
        }

        var remaining = args.Used.Comp.NextFire - _timing.CurTime;
        if (remaining <= TimeSpan.Zero)
            return;

        if (_timing.CurTime < ent.Comp.NextCooldownFeedbackAt)
            return;

        ent.Comp.NextCooldownFeedbackAt = _timing.CurTime + TimeSpan.FromSeconds(0.25);

        if (!TryComp(ent.Owner, out BuckleComponent? buckle) ||
            buckle.BuckledTo is not { } seat ||
            !HasComp<VehicleWeaponsSeatComponent>(seat))
        {
            return;
        }

        _ui.ServerSendUiMessage(
            seat,
            VehicleWeaponsUiKey.Key,
            new VehicleWeaponsCooldownFeedbackMessage((float) remaining.TotalSeconds),
            ent.Owner);

        _audio.PlayPredicted(args.Used.Comp.SoundEmpty, args.Used.Owner, ent.Owner);
    }

    private bool TrySelectHardpoint(EntityUid seat, EntityUid actor, EntityUid? mountedWeapon, bool fromUi)
    {
        if (_net.IsClient)
            return false;

        if (!_vehicleSystem.TryGetVehicleFromInterior(seat, out var vehicle) || vehicle == null)
            return false;

        var vehicleUid = vehicle.Value;
        if (!TryComp(vehicleUid, out VehicleWeaponsComponent? weapons))
            return false;

        if (!TryComp(actor, out BuckleComponent? buckle) ||
            buckle.BuckledTo != seat ||
            !TryComp(seat, out VehicleWeaponsSeatComponent? seatComp))
        {
            return false;
        }

        if (fromUi && !seatComp.AllowUiSelection)
            return false;

        if (!fromUi && !seatComp.AllowHotbarSelection)
            return false;

        if (TryComp(actor, out VehiclePortGunOperatorComponent? portGunOperator) &&
            portGunOperator.Gun != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-portgun-active"), seat, actor);
            return true;
        }

        if (!TryComp(vehicleUid, out HardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid, out ItemSlotsComponent? itemSlots))
        {
            return false;
        }

        if (!TryComp(actor, out VehicleWeaponsOperatorComponent? operatorComp))
            return false;

        if (mountedWeapon == null)
        {
            ClearOperatorSelections(weapons, actor);
            RecalculateSelectedWeapon(vehicleUid, weapons, itemSlots);
            RefreshOperatorSelectedWeapons(vehicleUid, weapons, itemSlots);
            Dirty(vehicleUid, weapons);
            UpdateHardpointActionStates(actor, weapons, operatorComp);
            UpdateWeaponsUiForAllOperators(vehicleUid, weapons, hardpoints, itemSlots);
            return true;
        }

        if (!Exists(mountedWeapon.Value) ||
            !_topology.TryGetMountedSlotByItem(vehicleUid, mountedWeapon.Value, out var mountedSlot, hardpoints, itemSlots) ||
            !HasComp<GunComponent>(mountedWeapon.Value) ||
            !HasComp<VehicleTurretComponent>(mountedWeapon.Value) ||
            !TryGetMountedWeaponHardpointType(vehicleUid, mountedWeapon.Value, out var hardpointType, hardpoints, itemSlots) ||
            !IsHardpointTypeAllowed(seatComp, hardpointType))
        {
            return false;
        }

        var sharedSelection = IsSharedHardpointType(hardpointType);
        if (!sharedSelection &&
            weapons.HardpointOperators.TryGetValue(mountedWeapon.Value, out var currentOperator) &&
            currentOperator != actor)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-weapons-ui-hardpoint-in-use", ("operator", currentOperator)), seat, actor);
            UpdateWeaponsUiForAllOperators(vehicleUid, weapons, hardpoints, itemSlots);
            return true;
        }

        var playSelectSound = !weapons.OperatorSelections.TryGetValue(actor, out var priorWeapon) ||
                              priorWeapon != mountedWeapon.Value;

        if (weapons.OperatorSelections.TryGetValue(actor, out var existingWeapon) &&
            existingWeapon == mountedWeapon.Value)
        {
            weapons.OperatorSelections.Remove(actor);
            if (!sharedSelection &&
                weapons.HardpointOperators.TryGetValue(mountedWeapon.Value, out var existingOperator) &&
                existingOperator == actor)
            {
                weapons.HardpointOperators.Remove(mountedWeapon.Value);
            }
        }
        else
        {
            if (weapons.OperatorSelections.TryGetValue(actor, out var previousWeapon) &&
                weapons.HardpointOperators.TryGetValue(previousWeapon, out var existingOperator) &&
                existingOperator == actor)
            {
                weapons.HardpointOperators.Remove(previousWeapon);
            }

            weapons.OperatorSelections[actor] = mountedWeapon.Value;
            if (!sharedSelection)
                weapons.HardpointOperators[mountedWeapon.Value] = actor;

            if (playSelectSound &&
                TryComp(mountedWeapon.Value, out GunSpinupComponent? spinup) &&
                spinup.SelectSound != null)
            {
                _audio.PlayPredicted(spinup.SelectSound, mountedWeapon.Value, actor);
            }
        }

        RecalculateSelectedWeapon(vehicleUid, weapons, itemSlots);
        RefreshOperatorSelectedWeapons(vehicleUid, weapons, itemSlots);
        Dirty(vehicleUid, weapons);
        UpdateHardpointActionStates(actor, weapons, operatorComp);
        UpdateWeaponsUiForAllOperators(vehicleUid, weapons, hardpoints, itemSlots);
        return true;
    }

    private void OnHardpointSlotsChanged(HardpointSlotsChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(args.Vehicle, out VehicleWeaponsComponent? weapons))
            return;

        HardpointSlotsComponent? hardpoints = null;
        ItemSlotsComponent? itemSlots = null;

        if (weapons.SelectedWeapon is { } selected &&
            Resolve(args.Vehicle, ref hardpoints, logMissing: false) &&
            Resolve(args.Vehicle, ref itemSlots, logMissing: false) &&
            !IsSelectedWeaponInstalled(args.Vehicle, selected, hardpoints, itemSlots))
        {
            weapons.SelectedWeapon = null;
            Dirty(args.Vehicle, weapons);
        }

        PruneHardpointOperators(args.Vehicle, weapons, hardpoints, itemSlots);
        RecalculateSelectedWeapon(args.Vehicle, weapons, itemSlots);
        RefreshOperatorSelectedWeapons(args.Vehicle, weapons, itemSlots);
        RefreshSeatGunnerViews(args.Vehicle);
        Dirty(args.Vehicle, weapons);

        UpdateWeaponsUiForAllOperators(args.Vehicle, weapons, hardpoints, itemSlots, refreshActions: true);
    }

    private void RefreshSeatGunnerViews(EntityUid vehicle)
    {
        var query = EntityQueryEnumerator<VehicleWeaponsOperatorComponent>();
        while (query.MoveNext(out var user, out var op))
        {
            if (op.Vehicle != vehicle)
                continue;

            if (!TryGetUserWeaponsSeat(user, out _, out var seatComp))
                continue;

            UpdateGunnerView(user, vehicle, seatComp);
        }
    }

    private void UpdateGunnerView(
        EntityUid user,
        EntityUid vehicle,
        VehicleWeaponsSeatComponent? seatComp = null,
        bool removeOnly = false)
    {
        seatComp ??= CompOrNull<VehicleWeaponsSeatComponent>(Transform(user).ParentUid);

        if (removeOnly)
        {
            if (RemCompDeferred<VehicleGunnerViewUserComponent>(user))
                _eyeSystem.UpdatePvsScale(user);

            return;
        }

        var hasView = false;
        var pvsScale = 0f;
        var cursorMaxOffset = 0f;
        var cursorOffsetSpeed = 0.5f;
        var cursorPvsIncrease = 0f;

        if (seatComp != null && HasBaseGunnerView(seatComp))
        {
            pvsScale = Math.Max(pvsScale, seatComp.BaseViewPvsScale);
            cursorMaxOffset = Math.Max(cursorMaxOffset, seatComp.BaseViewCursorMaxOffset);
            cursorOffsetSpeed = MathF.Max(cursorOffsetSpeed, seatComp.BaseViewCursorOffsetSpeed);
            cursorPvsIncrease = Math.Max(cursorPvsIncrease, seatComp.BaseViewCursorPvsIncrease);
            hasView = true;
        }

        if (seatComp != null &&
            (seatComp.IsPrimaryOperatorSeat || HasBaseGunnerView(seatComp)) &&
            TryComp(vehicle, out VehicleGunnerViewComponent? gunnerView) &&
            gunnerView.PvsScale > 0f)
        {
            pvsScale = Math.Max(pvsScale, gunnerView.PvsScale);
            cursorMaxOffset = Math.Max(cursorMaxOffset, gunnerView.CursorMaxOffset);
            cursorOffsetSpeed = MathF.Max(cursorOffsetSpeed, gunnerView.CursorOffsetSpeed);
            cursorPvsIncrease = Math.Max(cursorPvsIncrease, gunnerView.CursorPvsIncrease);
            hasView = true;
        }

        if (hasView && pvsScale > 0f)
        {
            var view = EnsureComp<VehicleGunnerViewUserComponent>(user);
            view.PvsScale = pvsScale;
            view.CursorMaxOffset = cursorMaxOffset;
            view.CursorOffsetSpeed = cursorOffsetSpeed;
            view.CursorPvsIncrease = cursorPvsIncrease;
            Dirty(user, view);
            _eyeSystem.UpdatePvsScale(user);
            return;
        }

        if (RemCompDeferred<VehicleGunnerViewUserComponent>(user))
            _eyeSystem.UpdatePvsScale(user);
    }

    private static bool HasBaseGunnerView(VehicleWeaponsSeatComponent seatComp)
    {
        return seatComp.BaseViewPvsScale > 0f ||
               seatComp.BaseViewCursorMaxOffset > 0f ||
               seatComp.BaseViewCursorPvsIncrease > 0f;
    }

    private bool IsSelectedWeaponInstalled(EntityUid vehicle, EntityUid selected, HardpointSlotsComponent hardpoints, ItemSlotsComponent itemSlots)
    {
        foreach (var mountedSlot in _topology.GetMountedSlots(vehicle, hardpoints, itemSlots))
        {
            if (mountedSlot.Item == selected)
                return true;
        }

        return false;
    }

    private void OnTurretGunShot(Entity<VehicleTurretComponent> ent, ref GunShotEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetContainingVehicle(ent.Owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out VehicleWeaponsComponent? weapons))
            return;

        UpdateWeaponsUiForAllOperators(vehicle, weapons);
    }

    private bool TryGetContainingVehicle(EntityUid owner, out EntityUid vehicle)
    {
        return _topology.TryGetVehicle(owner, out vehicle);
    }

    private void ClearOperatorSelections(VehicleWeaponsComponent weapons, EntityUid operatorUid)
    {
        weapons.OperatorSelections.Remove(operatorUid);

        foreach (var pair in weapons.HardpointOperators.ToArray())
        {
            if (pair.Value == operatorUid)
                weapons.HardpointOperators.Remove(pair.Key);
        }
    }

    private void PruneHardpointOperators(
        EntityUid vehicle,
        VehicleWeaponsComponent weapons,
        HardpointSlotsComponent? hardpoints,
        ItemSlotsComponent? itemSlots)
    {
        if (!Resolve(vehicle, ref hardpoints, logMissing: false))
            return;

        foreach (var entry in weapons.HardpointOperators.ToArray())
        {
            if (!Exists(entry.Key) ||
                !Exists(entry.Value) ||
                !_topology.TryGetMountedSlotByItem(vehicle, entry.Key, out _, hardpoints, itemSlots))
            {
                weapons.HardpointOperators.Remove(entry.Key);
            }
        }

        foreach (var entry in weapons.OperatorSelections.ToArray())
        {
            if (!Exists(entry.Key) ||
                !Exists(entry.Value) ||
                !_topology.TryGetMountedSlotByItem(vehicle, entry.Value, out _, hardpoints, itemSlots))
            {
                weapons.OperatorSelections.Remove(entry.Key);
            }
        }
    }

    private bool TryGetUserWeaponsSeat(
        EntityUid user,
        out EntityUid seat,
        out VehicleWeaponsSeatComponent seatComp)
    {
        seat = default;
        seatComp = default!;

        if (!TryComp(user, out BuckleComponent? buckle) ||
            buckle.BuckledTo is not { } buckledSeat ||
            !TryComp(buckledSeat, out VehicleWeaponsSeatComponent? resolvedSeatComp))
        {
            return false;
        }

        seatComp = resolvedSeatComp;
        seat = buckledSeat;
        return true;
    }

    private bool TryGetMountedWeaponHardpointType(
        EntityUid vehicle,
        EntityUid mountedWeapon,
        out string hardpointType)
    {
        return TryGetMountedWeaponHardpointType(vehicle, mountedWeapon, out hardpointType, hardpoints: null, itemSlots: null);
    }

    private bool TryGetMountedWeaponHardpointType(
        EntityUid vehicle,
        EntityUid mountedWeapon,
        out string hardpointType,
        HardpointSlotsComponent? hardpoints,
        ItemSlotsComponent? itemSlots)
    {
        hardpointType = string.Empty;

        if (!_topology.TryGetMountedSlotByItem(vehicle, mountedWeapon, out var mountedSlot, hardpoints, itemSlots))
            return false;

        hardpointType = mountedSlot.HardpointType;
        return true;
    }

    private bool IsHardpointTypeAllowed(VehicleWeaponsSeatComponent seatComp, string hardpointType)
    {
        if (seatComp.AllowedHardpointTypes.Count == 0)
            return true;

        foreach (var allowed in seatComp.AllowedHardpointTypes)
        {
            if (string.Equals(allowed, hardpointType, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsSharedHardpointType(string hardpointType)
    {
        return string.Equals(hardpointType, "Support", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshOperatorSelectedWeapons(
        EntityUid vehicle,
        VehicleWeaponsComponent weapons,
        ItemSlotsComponent? itemSlots = null)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<VehicleWeaponsOperatorComponent>();
        while (query.MoveNext(out var operatorUid, out var operatorComp))
        {
            if (operatorComp.Vehicle != vehicle)
                continue;

            EntityUid? selectedWeapon = null;
            if (weapons.OperatorSelections.TryGetValue(operatorUid, out var operatorSelectedWeapon) &&
                IsSelectableMountedWeapon(vehicle, operatorSelectedWeapon, itemSlots: itemSlots))
            {
                selectedWeapon = operatorSelectedWeapon;
            }

            if (operatorComp.SelectedWeapon == selectedWeapon)
                continue;

            operatorComp.SelectedWeapon = selectedWeapon;
            Dirty(operatorUid, operatorComp);
        }
    }

    public bool TryGetSelectedWeaponForOperator(EntityUid vehicle, EntityUid operatorUid, out EntityUid weapon)
    {
        weapon = default;

        if (!TryComp(vehicle, out VehicleWeaponsComponent? weapons))
        {
            return false;
        }

        if (weapons.OperatorSelections.TryGetValue(operatorUid, out var selectedWeapon) &&
            IsSelectableMountedWeapon(vehicle, selectedWeapon))
        {
            weapon = selectedWeapon;
            return true;
        }

        if (TryComp(operatorUid, out VehicleWeaponsOperatorComponent? operatorComp) &&
            operatorComp.Vehicle == vehicle &&
            operatorComp.SelectedWeapon is { } operatorWeapon &&
            Exists(operatorWeapon) &&
            HasComp<GunComponent>(operatorWeapon))
        {
            weapon = operatorWeapon;
            return true;
        }

        if (weapons.Operator == operatorUid &&
            weapons.SelectedWeapon is { } primaryWeapon &&
            Exists(primaryWeapon) &&
            HasComp<GunComponent>(primaryWeapon))
        {
            weapon = primaryWeapon;
            return true;
        }

        return false;
    }

    public bool TryGetOperatorForSelectedWeapon(EntityUid vehicle, EntityUid weapon, out EntityUid operatorUid)
    {
        operatorUid = default;

        if (!TryComp(vehicle, out VehicleWeaponsComponent? weapons))
        {
            return false;
        }

        foreach (var entry in weapons.OperatorSelections)
        {
            if (!Exists(entry.Key) ||
                entry.Value != weapon ||
                !IsSelectableMountedWeapon(vehicle, entry.Value))
            {
                continue;
            }

            operatorUid = entry.Key;
            return true;
        }

        var query = EntityQueryEnumerator<VehicleWeaponsOperatorComponent>();
        while (query.MoveNext(out var candidateUid, out var operatorComp))
        {
            if (operatorComp.Vehicle != vehicle ||
                operatorComp.SelectedWeapon != weapon)
            {
                continue;
            }

            operatorUid = candidateUid;
            return true;
        }

        return false;
    }

    private void RecalculateSelectedWeapon(
        EntityUid vehicle,
        VehicleWeaponsComponent weapons,
        ItemSlotsComponent? itemSlots = null)
    {
        if (weapons.Operator is not { } primaryOperator ||
            !weapons.OperatorSelections.TryGetValue(primaryOperator, out var selectedWeapon))
        {
            weapons.SelectedWeapon = null;
            return;
        }

        if (!IsSelectableMountedWeapon(vehicle, selectedWeapon, itemSlots: itemSlots))
        {
            weapons.SelectedWeapon = null;
            return;
        }

        weapons.SelectedWeapon = selectedWeapon;
    }

    private bool IsSelectableMountedWeapon(
        EntityUid vehicle,
        EntityUid mountedWeapon,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        return Exists(mountedWeapon) &&
               HasComp<VehicleTurretComponent>(mountedWeapon) &&
               HasComp<GunComponent>(mountedWeapon) &&
               _topology.TryGetMountedSlotByItem(vehicle, mountedWeapon, out _, hardpoints, itemSlots);
    }
}

