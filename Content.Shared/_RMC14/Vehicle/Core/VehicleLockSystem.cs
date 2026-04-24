using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleLockSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleEnterComponent, MapInitEvent>(OnVehicleMapInit);
        SubscribeLocalEvent<VehicleEnterComponent, InteractUsingEvent>(OnVehicleInteractUsing);

        SubscribeLocalEvent<VehicleLockActionComponent, VehicleLockActionEvent>(OnLockAction);
        SubscribeLocalEvent<VehicleLockActionComponent, ComponentShutdown>(OnLockActionShutdown);
        SubscribeLocalEvent<VehicleLockComponent, VehicleLockBreakDoAfterEvent>(OnLockBreakDoAfter);
        SubscribeLocalEvent<VehicleLockComponent, VehicleLockRepairDoAfterEvent>(OnLockRepairDoAfter);
    }

    private void OnVehicleMapInit(Entity<VehicleEnterComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        EnsureComp<VehicleLockComponent>(ent.Owner);
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

        if (actionComp.Action != null)
        {
            _actions.RemoveAction(user, actionComp.Action.Value);
            actionComp.Action = null;
        }

        RemCompDeferred<VehicleLockActionComponent>(user);
    }

    private void OnLockActionShutdown(Entity<VehicleLockActionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Action is { } action)
            _actions.RemoveAction(action);
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
        if (lockComp.Broken)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vehicle-lock-broken-attempt"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            RefreshLockAction(vehicle, lockComp, ent.Comp);
            return;
        }

        lockComp.Locked = !lockComp.Locked;
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

        var lockComp = EnsureComp<VehicleLockComponent>(ent.Owner);

        if (!lockComp.Broken)
        {
            if (!_tool.HasQuality(args.Used, lockComp.BreakToolQuality))
                return;

            var doAfter = new DoAfterArgs(EntityManager, args.User, (float) lockComp.BreakDelay.TotalSeconds, new VehicleLockBreakDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnHandChange = true,
                NeedHand = true,
                RequireCanInteract = true,
                DuplicateCondition = DuplicateConditions.SameTool | DuplicateConditions.SameTarget,
            };

            if (!_doAfter.TryStartDoAfter(doAfter))
                return;

            args.Handled = true;
            return;
        }

        if (!_tool.HasQuality(args.Used, lockComp.RepairToolQuality))
            return;

        var repairDoAfter = new DoAfterArgs(EntityManager, args.User, (float) lockComp.RepairDelay.TotalSeconds, new VehicleLockRepairDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true,
            RequireCanInteract = true,
            DuplicateCondition = DuplicateConditions.SameTool | DuplicateConditions.SameTarget,
        };

        if (!_doAfter.TryStartDoAfter(repairDoAfter))
            return;

        args.Handled = true;
    }

    private void OnLockBreakDoAfter(Entity<VehicleLockComponent> ent, ref VehicleLockBreakDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled || ent.Comp.Broken)
            return;

        args.Handled = true;
        ent.Comp.Broken = true;
        ent.Comp.Locked = false;
        Dirty(ent);
        RefreshLockAction(ent.Owner, ent.Comp);
        _popup.PopupEntity(Loc.GetString("rmc-vehicle-lock-broken-success"), args.User, args.User, PopupType.Small);
    }

    private void OnLockRepairDoAfter(Entity<VehicleLockComponent> ent, ref VehicleLockRepairDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled || !ent.Comp.Broken)
            return;

        args.Handled = true;
        ent.Comp.Broken = false;
        ent.Comp.Locked = false;
        Dirty(ent);
        RefreshLockAction(ent.Owner, ent.Comp);
        _popup.PopupEntity(Loc.GetString("rmc-vehicle-lock-repaired"), args.User, args.User, PopupType.Small);
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
}
