using System;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NPC.Components;
using Content.Shared.Popups;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleLockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly VehicleSystem _vehicle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleEnterComponent, MapInitEvent>(OnVehicleMapInit);
        SubscribeLocalEvent<VehicleEnterComponent, InteractUsingEvent>(OnVehicleInteractUsing);
        SubscribeLocalEvent<VehicleExitComponent, InteractUsingEvent>(OnVehicleExitInteractUsing);
        SubscribeLocalEvent<VehicleKeyComponent, InteractUsingEvent>(OnKeyInteractUsing);
        SubscribeLocalEvent<VehicleKeyComponent, ExaminedEvent>(OnKeyExamined);

        SubscribeLocalEvent<VehicleLockActionComponent, VehicleLockActionEvent>(OnLockAction);
        SubscribeLocalEvent<VehicleLockActionComponent, ComponentShutdown>(OnLockActionShutdown);
    }

    public bool CanPassLock(Entity<VehicleLockComponent?> vehicle, EntityUid user)
    {
        if (!Resolve(vehicle, ref vehicle.Comp, false) || !vehicle.Comp.Locked)
            return true;

        if (HasComp<XenoComponent>(user) || _vehicle.IsVehicleFrameDestroyed(vehicle))
            return true;

        if (vehicle.Comp.Access.Count > 0)
        {
            var hasAccess = false;
            foreach (var tag in _accessReader.FindAccessTags(user))
            {
                if (!vehicle.Comp.Access.Contains(tag))
                    continue;

                hasAccess = true;
                break;
            }

            if (!hasAccess)
                return false;
        }

        return TryComp(user, out NpcFactionMemberComponent? member) &&
               vehicle.Comp.Factions.Overlaps(member.Factions);
    }

    public void SetFactionFromOccupant(EntityUid vehicle, EntityUid user)
    {
        var lockComp = EnsureComp<VehicleLockComponent>(vehicle);
        lockComp.Factions.Clear();
        if (TryComp(user, out NpcFactionMemberComponent? member))
            lockComp.Factions.UnionWith(member.Factions);

        Dirty(vehicle, lockComp);
    }

    private void OnVehicleMapInit(Entity<VehicleEnterComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        EnsureVehicleKeyId(ent.Owner);
    }

    public void EnableLockAction(EntityUid user, EntityUid vehicle)
    {
        var actionComp = EnsureComp<VehicleLockActionComponent>(user);
        actionComp.Sources.Add(vehicle);
        actionComp.Vehicle = vehicle;

        var lockComp = EnsureComp<VehicleLockComponent>(vehicle);

        if (actionComp.Action == null || TerminatingOrDeleted(actionComp.Action.Value))
            actionComp.Action = _actions.AddAction(user, actionComp.ActionId);

        if (actionComp.Action is { } actionUid)
        {
            _actions.SetEnabled(actionUid, true);
            _actions.SetToggled(actionUid, lockComp.Locked);
        }

        Dirty(user, actionComp);
    }

    public void DisableLockAction(EntityUid user, EntityUid vehicle)
    {
        if (!TryComp(user, out VehicleLockActionComponent? actionComp))
            return;

        actionComp.Sources.Remove(vehicle);
        if (actionComp.Sources.Count > 0)
        {
            foreach (var remaining in actionComp.Sources)
            {
                actionComp.Vehicle = remaining;
                break;
            }

            if (actionComp.Action is { } actionUid &&
                actionComp.Vehicle is { } actionVehicle &&
                TryComp(actionVehicle, out VehicleLockComponent? lockComp))
            {
                _actions.SetToggled(actionUid, lockComp.Locked);
            }

            Dirty(user, actionComp);
            return;
        }

        if (actionComp.Action is { } action)
        {
            RemoveAndDeleteLockAction(user, action);
            actionComp.Action = null;
        }

        RemCompDeferred<VehicleLockActionComponent>(user);
    }

    private void OnLockActionShutdown(Entity<VehicleLockActionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Action is { } action)
            RemoveAndDeleteLockAction(ent.Owner, action);
    }

    private void RemoveAndDeleteLockAction(EntityUid user, EntityUid action)
    {
        if (TerminatingOrDeleted(action))
            return;

        _actions.RemoveAction(user, action);

        if (_net.IsClient)
            return;

        if (Exists(action))
            QueueDel(action);
    }

    private void OnLockAction(Entity<VehicleLockActionComponent> ent, ref VehicleLockActionEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;

        if (ent.Comp.Vehicle is not { } vehicle || Deleted(vehicle))
        {
            return;
        }

        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator != ent.Owner)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-lock-not-driver"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        var lockComp = EnsureComp<VehicleLockComponent>(vehicle);
        lockComp.Locked = !lockComp.Locked;
        Dirty(vehicle, lockComp);
        RefreshLockAction(vehicle, lockComp, ent.Comp);

        _popup.PopupEntity(
            Loc.GetString(lockComp.Locked ? "rmc-vehicle-lock-set-locked" : "rmc-vehicle-lock-set-unlocked"),
            ent.Owner,
            ent.Owner,
            PopupType.Small);
    }

    private void OnVehicleInteractUsing(Entity<VehicleEnterComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!TryComp(args.Used, out VehicleKeyComponent? keyComp))
            return;

        args.Handled = TryUseKeyOnVehicle((args.Used, keyComp), ent.Owner, args.User);
    }

    private void OnKeyInteractUsing(Entity<VehicleKeyComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!TryComp(args.Used, out VehicleKeyComponent? sourceKey) ||
            sourceKey.Mode != VehicleKeyMode.Duplicator)
        {
            return;
        }

        if (ent.Comp.KeyId == null)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-key-copy-invalid"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        BindKey((args.Used, sourceKey), ent.Comp.KeyId, copied: true, sourceName: Name(ent));
        _popup.PopupEntity(Loc.GetString("rmc-vehicle-key-copy-success"), args.User, args.User, PopupType.Small);
        args.Handled = true;
    }

    private void OnVehicleExitInteractUsing(Entity<VehicleExitComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (!TryComp(args.Used, out VehicleKeyComponent? keyComp))
            return;

        if (!_vehicle.TryGetVehicleFromInterior(ent.Owner, out var vehicle) || vehicle is not { } vehicleUid)
            return;

        args.Handled = TryUseKeyOnVehicle((args.Used, keyComp), vehicleUid, args.User);
    }

    private void OnKeyExamined(Entity<VehicleKeyComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = ent.Comp.Mode switch
        {
            VehicleKeyMode.Blank when ent.Comp.KeyId == null => "rmc-vehicle-key-examine-blank",
            VehicleKeyMode.Duplicator when ent.Comp.KeyId == null => "rmc-vehicle-key-examine-duplicator",
            _ => "rmc-vehicle-key-examine-bound",
        };

        args.PushMarkup(Loc.GetString(text));
    }

    private void RefreshLockAction(EntityUid vehicle, VehicleLockComponent lockComp, VehicleLockActionComponent? actionComp = null)
    {
        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) ||
            vehicleComp.Operator is not { } operatorUid ||
            !TryComp(operatorUid, out VehicleLockActionComponent? operatorAction))
        {
            return;
        }

        actionComp ??= operatorAction;

        if (actionComp.Action is not { } actionUid)
            return;

        _actions.SetEnabled(actionUid, true);
        _actions.SetToggled(actionUid, lockComp.Locked);
        Dirty(operatorUid, actionComp);
    }

    public string EnsureVehicleKeyId(EntityUid vehicle)
    {
        var lockComp = EnsureComp<VehicleLockComponent>(vehicle);
        if (!string.IsNullOrWhiteSpace(lockComp.KeyId))
            return lockComp.KeyId;

        lockComp.KeyId = Guid.NewGuid().ToString("N");
        Dirty(vehicle, lockComp);
        return lockComp.KeyId;
    }

    public bool TryUseKeyOnVehicle(Entity<VehicleKeyComponent> key, EntityUid vehicle, EntityUid user)
    {
        var vehicleLock = EnsureComp<VehicleLockComponent>(vehicle);
        var vehicleKeyId = EnsureVehicleKeyId(vehicle);

        if (key.Comp.KeyId == null)
        {
            switch (key.Comp.Mode)
            {
                case VehicleKeyMode.Blank:
                    BindKey(key, vehicleKeyId, vehicle);
                    _popup.PopupEntity(Loc.GetString("rmc-vehicle-key-bind-success"), user, user, PopupType.Small);
                    return true;
                case VehicleKeyMode.Duplicator:
                    _popup.PopupEntity(Loc.GetString("rmc-vehicle-key-copy-requires-source"), user, user, PopupType.SmallCaution);
                    return true;
                default:
                    _popup.PopupEntity(Loc.GetString("rmc-vehicle-key-unbound"), user, user, PopupType.SmallCaution);
                    return true;
            }
        }

        if (key.Comp.KeyId != vehicleKeyId)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-key-invalid"), user, user, PopupType.SmallCaution);
            return true;
        }

        vehicleLock.Locked = !vehicleLock.Locked;
        Dirty(vehicle, vehicleLock);
        RefreshLockAction(vehicle, vehicleLock);
        _popup.PopupEntity(
            Loc.GetString(vehicleLock.Locked ? "rmc-vehicle-lock-set-locked" : "rmc-vehicle-lock-set-unlocked"),
            user,
            user,
            PopupType.Small);
        return true;
    }

    public void BindKey(
        Entity<VehicleKeyComponent> key,
        string keyId,
        EntityUid? vehicle = null,
        bool copied = false,
        string? sourceName = null)
    {
        key.Comp.KeyId = keyId;
        Dirty(key.Owner, key.Comp);

        var vehicleName = sourceName;
        if (vehicleName == null && vehicle is { } vehicleUid)
            vehicleName = Name(vehicleUid);

        if (vehicleName != null)
        {
            _meta.SetEntityName(
                key.Owner,
                Loc.GetString(
                    copied ? "rmc-vehicle-key-name-copy-specific" : "rmc-vehicle-key-name-specific",
                    ("vehicle", vehicleName)));
            return;
        }

        _meta.SetEntityName(key.Owner, Loc.GetString(copied ? "rmc-vehicle-key-name-copy" : "rmc-vehicle-key-name"));
    }
}
