using System;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class HardpointSlotSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HardpointSystem _hardpoints = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HardpointSlotsComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<HardpointSlotsComponent, HardpointInsertDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<HardpointSlotsComponent, InteractUsingEvent>(OnSlotsInteractUsing, before: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<HardpointSlotsComponent, BoundUIOpenedEvent>(OnHardpointUiOpened);
        SubscribeLocalEvent<HardpointSlotsComponent, BoundUIClosedEvent>(OnHardpointUiClosed);
        SubscribeLocalEvent<HardpointSlotsComponent, HardpointRemoveMessage>(OnHardpointRemoveMessage);
        SubscribeLocalEvent<HardpointSlotsComponent, HardpointRemoveDoAfterEvent>(OnHardpointRemoveDoAfter);
    }

    private void OnInsertAttempt(Entity<HardpointSlotsComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (!_hardpoints.TryGetSlot(ent.Comp, args.Slot.ID, out var slot))
            return;

        if (ent.Comp.CompletingInserts.Contains(slot.Id))
            return;

        if (!_hardpoints.IsValidHardpoint(args.Item, ent.Comp, slot))
        {
            args.Cancelled = true;
            return;
        }

        if (slot.InsertDelay <= 0f)
            return;

        if (ent.Comp.PendingInsertUsers.Contains(args.User.Value))
        {
            args.Cancelled = true;
            return;
        }

        if (!ent.Comp.PendingInserts.Add(slot.Id))
        {
            args.Cancelled = true;
            return;
        }

        args.Cancelled = true;
        ent.Comp.PendingInsertUsers.Add(args.User.Value);

        var doAfter = new DoAfterArgs(EntityManager, args.User.Value, slot.InsertDelay, new HardpointInsertDoAfterEvent(slot.Id), ent.Owner, ent.Owner, args.Item)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnWeightlessMove = true,
            NeedHand = true,
            RequireCanInteract = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ent.Comp.PendingInserts.Remove(slot.Id);
            ent.Comp.PendingInsertUsers.Remove(args.User.Value);
        }
    }

    private void OnInsertDoAfter(Entity<HardpointSlotsComponent> ent, ref HardpointInsertDoAfterEvent args)
    {
        ent.Comp.PendingInserts.Remove(args.SlotId);
        ent.Comp.PendingInsertUsers.Remove(args.User);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } item || string.IsNullOrEmpty(args.SlotId))
            return;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return;

        if (!_hardpoints.TryGetSlot(ent.Comp, args.SlotId, out var hardpointSlot))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, args.SlotId, out var slot, itemSlots))
            return;

        if (!_hardpoints.IsValidHardpoint(item, ent.Comp, hardpointSlot))
            return;

        ent.Comp.CompletingInserts.Add(args.SlotId);
        _itemSlots.TryInsertFromHand(ent.Owner, slot, args.User, excludeUserAudio: false);
        ent.Comp.CompletingInserts.Remove(args.SlotId);
    }

    private void OnSlotsInteractUsing(Entity<HardpointSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == null)
            return;

        if (TryInsertTurretAttachment(ent, args.User, args.Used))
        {
            args.Handled = true;
            return;
        }

        if (!_tool.HasQuality(args.Used, ent.Comp.RemoveToolQuality))
            return;

        if (_ui.TryOpenUi(ent.Owner, HardpointUiKey.Key, args.User))
        {
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp);
            args.Handled = true;
        }
    }

    private void OnHardpointUiOpened(Entity<HardpointSlotsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        ent.Comp.LastUiError = null;
        _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp);
    }

    private void OnHardpointUiClosed(Entity<HardpointSlotsComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        ent.Comp.PendingRemovals.Clear();
        ent.Comp.LastUiError = null;
    }

    private void OnHardpointRemoveMessage(Entity<HardpointSlotsComponent> ent, ref HardpointRemoveMessage args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        TryStartHardpointRemoval(ent.Owner, ent.Comp, args.Actor, args.SlotId);
    }

    private void OnHardpointRemoveDoAfter(Entity<HardpointSlotsComponent> ent, ref HardpointRemoveDoAfterEvent args)
    {
        ent.Comp.PendingRemovals.Remove(args.SlotId);

        if (args.Cancelled || args.Handled)
        {
            if (args.Cancelled)
            {
                ent.Comp.LastUiError = "Hardpoint removal cancelled.";
                _hardpoints.SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            }

            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        args.Handled = true;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
        {
            ent.Comp.LastUiError = "Unable to access hardpoint slots.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!_hardpoints.TryGetSlot(ent.Comp, args.SlotId, out _))
        {
            ent.Comp.LastUiError = "That hardpoint slot is no longer available.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent.Owner, args.SlotId, out var itemSlot, itemSlots) || !itemSlot.HasItem)
        {
            ent.Comp.LastUiError = "No hardpoint is installed in that slot.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!_itemSlots.TryEjectToHands(ent.Owner, itemSlot, args.User, true))
        {
            ent.Comp.LastUiError = "Couldn't remove the hardpoint. Free a hand and try again.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        ent.Comp.LastUiError = null;
        _hardpoints.SetContainingVehicleUiError(ent.Owner, null);
        _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
        _hardpoints.UpdateContainingVehicleUi(ent.Owner);
        _hardpoints.RefreshCanRun(ent.Owner);
    }

    private void TryStartHardpointRemoval(
        EntityUid uid,
        HardpointSlotsComponent component,
        EntityUid user,
        string? slotId,
        EntityUid? uiOwnerUid = null,
        HardpointSlotsComponent? uiOwnerComp = null)
    {
        var rootCall = uiOwnerUid == null || uiOwnerComp == null;
        uiOwnerUid ??= uid;
        uiOwnerComp ??= component;

        void RefreshUi(ItemSlotsComponent? currentItemSlots = null)
        {
            _hardpoints.UpdateHardpointUi(uid, component, currentItemSlots);

            if (uiOwnerUid.Value != uid || !ReferenceEquals(uiOwnerComp, component))
                _hardpoints.UpdateHardpointUi(uiOwnerUid.Value, uiOwnerComp);
        }

        void SetError(string error)
        {
            uiOwnerComp.LastUiError = error;
        }

        if (rootCall)
            uiOwnerComp.LastUiError = null;

        if (string.IsNullOrWhiteSpace(slotId))
        {
            SetError("Invalid hardpoint slot.");
            RefreshUi();
            return;
        }

        if (VehicleTurretSlotIds.TryParse(slotId, out var parentSlotId, out var childSlotId))
        {
            if (!TryComp(uid, out ItemSlotsComponent? parentItemSlots) ||
                !_hardpoints.TryGetSlot(component, parentSlotId, out _))
            {
                SetError("Unable to find that turret slot.");
                RefreshUi(parentItemSlots);
                return;
            }

            if (!_itemSlots.TryGetSlot(uid, parentSlotId, out var parentSlot, parentItemSlots) || !parentSlot.HasItem)
            {
                SetError("Install a turret before removing turret hardpoints.");
                RefreshUi(parentItemSlots);
                return;
            }

            var turretUid = parentSlot.Item!.Value;
            if (!TryComp(turretUid, out HardpointSlotsComponent? parentTurretSlots))
            {
                SetError("Turret hardpoint slots are unavailable.");
                RefreshUi(parentItemSlots);
                return;
            }

            TryStartHardpointRemoval(turretUid, parentTurretSlots, user, childSlotId, uiOwnerUid, uiOwnerComp);
            RefreshUi(parentItemSlots);
            return;
        }

        if (!TryComp(uid, out ItemSlotsComponent? itemSlots))
        {
            SetError("Unable to access hardpoint slots.");
            RefreshUi();
            return;
        }

        if (!_hardpoints.TryGetSlot(component, slotId, out var slot))
        {
            SetError("That hardpoint slot does not exist.");
            RefreshUi(itemSlots);
            return;
        }

        if (!_itemSlots.TryGetSlot(uid, slotId, out var itemSlot, itemSlots) || !itemSlot.HasItem)
        {
            SetError("No hardpoint is installed in that slot.");
            RefreshUi(itemSlots);
            return;
        }

        if (TryComp(itemSlot.Item!.Value, out HardpointSlotsComponent? attachedSlots) &&
            TryComp(itemSlot.Item!.Value, out ItemSlotsComponent? attachedItemSlots) &&
            _hardpoints.HasAttachedHardpoints(itemSlot.Item!.Value, attachedSlots, attachedItemSlots))
        {
            const string error = "Remove the turret attachments before removing the turret.";
            _popup.PopupEntity(error, uid, user);
            SetError(error);
            RefreshUi(itemSlots);
            return;
        }

        if (HasComp<HardpointNoRemoveComponent>(itemSlot.Item!.Value))
        {
            var error = Loc.GetString("rmc-hardpoint-remove-blocked");
            _popup.PopupEntity(error, uid, user);
            SetError(error);
            RefreshUi(itemSlots);
            return;
        }

        if (component.PendingInserts.Contains(slotId) || component.CompletingInserts.Contains(slotId))
        {
            const string error = "Finish installing that hardpoint before removing it.";
            _popup.PopupEntity(error, user, user);
            SetError(error);
            RefreshUi(itemSlots);
            return;
        }

        if (!_hardpoints.TryGetPryingTool(user, component.RemoveToolQuality, out var tool))
        {
            const string error = "You need a prying tool to remove this hardpoint.";
            _popup.PopupEntity(error, user, user);
            SetError(error);
            RefreshUi(itemSlots);
            return;
        }

        if (!component.PendingRemovals.Add(slotId))
        {
            SetError("That hardpoint is already being removed.");
            RefreshUi(itemSlots);
            return;
        }

        var delay = slot.RemoveDelay > 0f ? slot.RemoveDelay : slot.InsertDelay;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, new HardpointRemoveDoAfterEvent(slotId), uid, uid, tool)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnWeightlessMove = true,
            NeedHand = true,
            RequireCanInteract = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            component.PendingRemovals.Remove(slotId);
            SetError("Couldn't start hardpoint removal.");
            RefreshUi(itemSlots);
            return;
        }

        uiOwnerComp.LastUiError = null;
        RefreshUi(itemSlots);
    }

    private bool TryInsertTurretAttachment(Entity<HardpointSlotsComponent> ent, EntityUid user, EntityUid used)
    {
        if (!HasComp<HardpointItemComponent>(used))
            return false;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return false;

        var requiresTurret = HasComp<VehicleTurretAttachmentComponent>(used);
        var hasMatchingEmptySlot = false;

        foreach (var slot in ent.Comp.Slots)
        {
            if (!_hardpoints.IsValidHardpoint(used, ent.Comp, slot))
                continue;

            if (_itemSlots.TryGetSlot(ent.Owner, slot.Id, out var vehicleSlot, itemSlots) &&
                !vehicleSlot.HasItem)
            {
                hasMatchingEmptySlot = true;
                break;
            }
        }

        if (!requiresTurret && hasMatchingEmptySlot)
            return false;

        foreach (var slot in ent.Comp.Slots)
        {
            if (!_itemSlots.TryGetSlot(ent.Owner, slot.Id, out var vehicleSlot, itemSlots) || !vehicleSlot.HasItem)
                continue;

            var turretUid = vehicleSlot.Item!.Value;
            if (!TryComp(turretUid, out HardpointSlotsComponent? turretSlots) ||
                !TryComp(turretUid, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (!_hardpoints.IsValidHardpoint(used, turretSlots, turretSlot))
                    continue;

                if (!_itemSlots.TryGetSlot(turretUid, turretSlot.Id, out var turretItemSlot, turretItemSlots))
                    continue;

                if (turretItemSlot.HasItem)
                    continue;

                _itemSlots.TryInsertFromHand(turretUid, turretItemSlot, user);
                return true;
            }
        }

        if (requiresTurret)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-turret-no-base"), ent.Owner, user);
            return true;
        }

        return false;
    }

}
