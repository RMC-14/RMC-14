using System;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
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

    private HardpointStateComponent EnsureState(EntityUid uid)
    {
        return EnsureComp<HardpointStateComponent>(uid);
    }

    private void CleanupStaleInsertTracking(EntityUid vehicle, HardpointStateComponent state, string reason)
    {
        if (state.CompletingInserts.Count > 0)
            return;

        if (state.PendingInserts.Count == 0 && state.PendingInsertUsers.Count == 0)
            return;

        if (state.PendingInserts.Count > 0 && state.PendingInsertUsers.Count > 0)
            return;

        state.PendingInserts.Clear();
        state.PendingInsertUsers.Clear();
    }

    private void OnInsertAttempt(Entity<HardpointSlotsComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User == null)
            return;

        var state = EnsureState(ent.Owner);
        CleanupStaleInsertTracking(ent.Owner, state, "insert-attempt");

        if (!_hardpoints.TryGetSlot(ent.Comp, args.Slot.ID, out var slot))
            return;

        if (state.CompletingInserts.Contains(slot.Id))
            return;

        if (!_hardpoints.IsValidHardpoint(args.Item, ent.Comp, slot))
        {
            args.Cancelled = true;
            return;
        }

        if (slot.InsertDelay <= 0f)
            return;

        args.Cancelled = true;
    }

    private void OnInsertDoAfter(Entity<HardpointSlotsComponent> ent, ref HardpointInsertDoAfterEvent args)
    {
        var state = EnsureState(ent.Owner);
        state.PendingInserts.Remove(args.SlotId);
        state.PendingInsertUsers.Remove(args.User);
        CleanupStaleInsertTracking(ent.Owner, state, "insert-doafter");

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } item || string.IsNullOrEmpty(args.SlotId))
            return;

        if (!_hardpoints.TryResolveSlotLocation(ent.Owner, ent.Comp, args.SlotId, out var location))
            return;

        if (!_hardpoints.IsValidHardpoint(item, location.Slots, location.Definition))
            return;

        state.CompletingInserts.Add(location.Definition.Id);
        _itemSlots.TryInsert(location.Owner, location.Slot, item, args.User, excludeUserAudio: false);
        state.CompletingInserts.Remove(location.Definition.Id);
        CleanupStaleInsertTracking(ent.Owner, state, "insert-finished");
    }

    private void OnSlotsInteractUsing(Entity<HardpointSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == null)
            return;

        if (TryStartHardpointInsert(ent, args.User, args.Used))
        {
            args.Handled = true;
            return;
        }

        if (HasComp<HardpointItemComponent>(args.Used) &&
            HasComp<VehicleTurretAttachmentComponent>(args.Used))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-turret-no-base"), ent.Owner, args.User);
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

    private bool TryStartHardpointInsert(Entity<HardpointSlotsComponent> ent, EntityUid user, EntityUid used)
    {
        if (!HasComp<HardpointItemComponent>(used))
            return false;

        var state = EnsureState(ent.Owner);
        CleanupStaleInsertTracking(ent.Owner, state, "interact-using");

        if (!_hardpoints.TryFindEmptyInstallLocation(ent.Owner, ent.Comp, used, out var targetLocation))
            return false;

        if (targetLocation.Definition.InsertDelay <= 0f)
        {
            targetLocation.State.CompletingInserts.Add(targetLocation.Definition.Id);
            _itemSlots.TryInsertFromHand(targetLocation.Owner, targetLocation.Slot, user);
            targetLocation.State.CompletingInserts.Remove(targetLocation.Definition.Id);
            CleanupStaleInsertTracking(targetLocation.Owner, targetLocation.State, "instant-insert");
            return true;
        }

        if (EntityManager.IsClientSide(ent.Owner))
            return true;

        if (targetLocation.State.PendingInsertUsers.Contains(user))
            return true;

        if (!targetLocation.State.PendingInserts.Add(targetLocation.Definition.Id))
            return true;

        targetLocation.State.PendingInsertUsers.Add(user);

        var doAfter = new DoAfterArgs(EntityManager, user, targetLocation.Definition.InsertDelay, new HardpointInsertDoAfterEvent(targetLocation.Definition.Id), targetLocation.Owner, targetLocation.Owner, used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnWeightlessMove = true,
            NeedHand = true,
            RequireCanInteract = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            targetLocation.State.PendingInserts.Remove(targetLocation.Definition.Id);
            targetLocation.State.PendingInsertUsers.Remove(user);
            return true;
        }

        return true;
    }

    private void OnHardpointUiOpened(Entity<HardpointSlotsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        var state = EnsureState(ent.Owner);
        state.LastUiError = null;
        _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, state: state);
    }

    private void OnHardpointUiClosed(Entity<HardpointSlotsComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        var state = EnsureState(ent.Owner);
        state.PendingRemovals.Clear();
        state.LastUiError = null;
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
        var state = EnsureState(ent.Owner);
        state.PendingRemovals.Remove(args.SlotId);

        if (args.Cancelled || args.Handled)
        {
            if (args.Cancelled)
            {
                state.LastUiError = "Hardpoint removal cancelled.";
                _hardpoints.SetContainingVehicleUiError(ent.Owner, state.LastUiError);
            }

            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, state: state);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        args.Handled = true;

        if (!_hardpoints.TryResolveSlotLocation(ent.Owner, ent.Comp, args.SlotId, out var location))
        {
            state.LastUiError = "Unable to access hardpoint slots.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, state.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, state: state);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (location.Slot.Item is not { } installed)
        {
            state.LastUiError = "No hardpoint is installed in that slot.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, state.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, location.ItemSlots, state);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!_itemSlots.TryEjectToHands(location.Owner, location.Slot, args.User, true))
        {
            state.LastUiError = "Couldn't remove the hardpoint. Free a hand and try again.";
            _hardpoints.SetContainingVehicleUiError(ent.Owner, state.LastUiError);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, location.ItemSlots, state);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        state.LastUiError = null;
        _hardpoints.SetContainingVehicleUiError(ent.Owner, null);
        _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, location.ItemSlots, state);
        _hardpoints.UpdateContainingVehicleUi(ent.Owner);
        _hardpoints.RefreshCanRun(ent.Owner);
    }

    private void TryStartHardpointRemoval(
        EntityUid uid,
        HardpointSlotsComponent component,
        EntityUid user,
        string? slotId,
        EntityUid? uiOwnerUid = null)
    {
        uiOwnerUid ??= uid;
        var uiOwnerState = EnsureState(uiOwnerUid.Value);

        void RefreshUi()
        {
            _hardpoints.UpdateHardpointUi(uiOwnerUid.Value, state: uiOwnerState);
        }

        void SetError(string error)
        {
            uiOwnerState.LastUiError = error;
        }

        uiOwnerState.LastUiError = null;

        if (string.IsNullOrWhiteSpace(slotId))
        {
            SetError("Invalid hardpoint slot.");
            RefreshUi();
            return;
        }

        if (!_hardpoints.TryResolveSlotLocation(uid, component, slotId, out var location))
        {
            SetError("That hardpoint slot does not exist.");
            RefreshUi();
            return;
        }

        if (location.Slot.Item is not { } installed)
        {
            SetError("No hardpoint is installed in that slot.");
            RefreshUi();
            return;
        }

        if (TryComp(installed, out HardpointSlotsComponent? attachedSlots) &&
            TryComp(installed, out ItemSlotsComponent? attachedItemSlots) &&
            _hardpoints.HasAttachedHardpoints(installed, attachedSlots, attachedItemSlots))
        {
            const string error = "Remove the turret attachments before removing the turret.";
            _popup.PopupEntity(error, location.Owner, user);
            SetError(error);
            RefreshUi();
            return;
        }

        if (HasComp<HardpointNoRemoveComponent>(installed))
        {
            var error = Loc.GetString("rmc-hardpoint-remove-blocked");
            _popup.PopupEntity(error, location.Owner, user);
            SetError(error);
            RefreshUi();
            return;
        }

        if (location.State.PendingInserts.Contains(location.Definition.Id) ||
            location.State.CompletingInserts.Contains(location.Definition.Id))
        {
            const string error = "Finish installing that hardpoint before removing it.";
            _popup.PopupEntity(error, user, user);
            SetError(error);
            RefreshUi();
            return;
        }

        if (!_hardpoints.TryGetPryingTool(user, location.Slots.RemoveToolQuality, out var tool))
        {
            const string error = "You need a prying tool to remove this hardpoint.";
            _popup.PopupEntity(error, user, user);
            SetError(error);
            RefreshUi();
            return;
        }

        if (!location.State.PendingRemovals.Add(location.Definition.Id))
        {
            SetError("That hardpoint is already being removed.");
            RefreshUi();
            return;
        }

        var delay = location.Definition.RemoveDelay > 0f ? location.Definition.RemoveDelay : location.Definition.InsertDelay;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, new HardpointRemoveDoAfterEvent(location.Definition.Id), location.Owner, location.Owner, tool)
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
            location.State.PendingRemovals.Remove(location.Definition.Id);
            SetError("Couldn't start hardpoint removal.");
            RefreshUi();
            return;
        }

        uiOwnerState.LastUiError = null;
        RefreshUi();
    }

}
