using System.Collections.Generic;
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

    private readonly HashSet<(EntityUid Owner, string SlotId)> _completingRemovals = new();

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
        SubscribeLocalEvent<HardpointSlotsComponent, ItemSlotEjectAttemptEvent>(OnHardpointEjectAttempt);
    }

    private void OnInsertAttempt(Entity<HardpointSlotsComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User == null)
            return;

        var state = EnsureComp<HardpointStateComponent>(ent.Owner);

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
        HardpointSlotLocation? resolvedLocation = null;
        if (_hardpoints.TryResolveSlotLocation(ent.Owner, ent.Comp, args.SlotId, out var location))
        {
            resolvedLocation = location;
            location.State.PendingInserts.Remove(location.Definition.Id);
        }
        else
        {
            EnsureComp<HardpointStateComponent>(ent.Owner).PendingInserts.Remove(args.SlotId);
        }

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } item || string.IsNullOrEmpty(args.SlotId))
            return;

        if (resolvedLocation is not { } finalLocation &&
            !_hardpoints.TryResolveSlotLocation(ent.Owner, ent.Comp, args.SlotId, out finalLocation))
        {
            return;
        }

        if (!_hardpoints.IsValidHardpoint(item, finalLocation.Slots, finalLocation.Definition))
            return;

        finalLocation.State.CompletingInserts.Add(finalLocation.Definition.Id);
        _itemSlots.TryInsert(finalLocation.Owner, finalLocation.Slot, item, args.User, excludeUserAudio: false);
        finalLocation.State.CompletingInserts.Remove(finalLocation.Definition.Id);
    }

    private void OnSlotsInteractUsing(Entity<HardpointSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
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

        if (!_hardpoints.TryFindEmptyInstallLocation(ent.Owner, ent.Comp, used, out var targetLocation))
            return false;

        if (targetLocation.Definition.InsertDelay <= 0f)
        {
            targetLocation.State.CompletingInserts.Add(targetLocation.Definition.Id);
            _itemSlots.TryInsertFromHand(targetLocation.Owner, targetLocation.Slot, user);
            targetLocation.State.CompletingInserts.Remove(targetLocation.Definition.Id);
            return true;
        }

        if (EntityManager.IsClientSide(ent.Owner))
            return true;

        if (targetLocation.State.PendingInserts.ContainsValue(user))
            return true;

        if (!targetLocation.State.PendingInserts.TryAdd(targetLocation.Definition.Id, user))
            return true;

        var slotId = targetLocation.Path.ToCompositeId();
        var doAfter = new DoAfterArgs(EntityManager, user, targetLocation.Definition.InsertDelay, new HardpointInsertDoAfterEvent(slotId), ent.Owner, ent.Owner, used)
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
            return true;
        }

        return true;
    }

    private void OnHardpointUiOpened(Entity<HardpointSlotsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        var state = EnsureComp<HardpointStateComponent>(ent.Owner);
        state.LastUiError = null;
        _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, state: state);
    }

    private void OnHardpointUiClosed(Entity<HardpointSlotsComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, HardpointUiKey.Key))
            return;

        var state = EnsureComp<HardpointStateComponent>(ent.Owner);
        state.PendingRemovals.Clear();
        state.LastUiError = null;
    }

    private void OnHardpointEjectAttempt(Entity<HardpointSlotsComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Slot.ID is not { } slotId)
            return;

        if (!_hardpoints.TryGetSlot(ent.Comp, slotId, out _))
            return;

        if (!_completingRemovals.Contains((ent.Owner, slotId)))
            args.Cancelled = true;
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
        var state = EnsureComp<HardpointStateComponent>(ent.Owner);

        void SetErrorAndRefresh(string? error)
        {
            state.LastUiError = error;
            _hardpoints.SetContainingVehicleUiError(ent.Owner, error);
            _hardpoints.UpdateHardpointUi(ent.Owner, ent.Comp, state: state);
            _hardpoints.UpdateContainingVehicleUi(ent.Owner);
        }

        HardpointSlotLocation? resolvedLocation = null;
        if (_hardpoints.TryResolveSlotLocation(ent.Owner, ent.Comp, args.SlotId, out var location))
        {
            resolvedLocation = location;
            location.State.PendingRemovals.Remove(location.Definition.Id);
        }
        else
        {
            state.PendingRemovals.Remove(args.SlotId);
        }

        if (args.Cancelled || args.Handled)
        {
            SetErrorAndRefresh(args.Cancelled ? "Hardpoint removal cancelled." : null);
            return;
        }

        args.Handled = true;

        if (resolvedLocation is not { } finalLocation &&
            !_hardpoints.TryResolveSlotLocation(ent.Owner, ent.Comp, args.SlotId, out finalLocation))
        {
            SetErrorAndRefresh("Unable to access hardpoint slots.");
            return;
        }

        if (!finalLocation.Slot.HasItem)
        {
            SetErrorAndRefresh("No hardpoint is installed in that slot.");
            return;
        }

        var key = (finalLocation.Owner, finalLocation.Definition.Id);
        _completingRemovals.Add(key);
        var ejected = _itemSlots.TryEjectToHands(finalLocation.Owner, finalLocation.Slot, args.User, true);
        _completingRemovals.Remove(key);

        if (!ejected)
        {
            SetErrorAndRefresh("Couldn't remove the hardpoint. Free a hand and try again.");
            return;
        }

        SetErrorAndRefresh(null);
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
        var uiOwnerState = EnsureComp<HardpointStateComponent>(uiOwnerUid.Value);

        void SetError(string error)
        {
            uiOwnerState.LastUiError = error;
        }

        void RefreshUi()
        {
            _hardpoints.UpdateHardpointUi(uiOwnerUid.Value, state: uiOwnerState);
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

        if (location.State.PendingInserts.ContainsKey(location.Definition.Id) ||
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
        var slotPath = location.Path.ToCompositeId();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, new HardpointRemoveDoAfterEvent(slotPath), uiOwnerUid.Value, uiOwnerUid.Value, tool)
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
