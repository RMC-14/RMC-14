using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCHardpointSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHardpointSlotsComponent, ComponentInit>(OnSlotsInit);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, MapInitEvent>(OnSlotsMapInit);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, VehicleCanRunEvent>(OnVehicleCanRun);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, RMCHardpointInsertDoAfterEvent>(OnInsertDoAfter);
    }

    private void OnSlotsInit(Entity<RMCHardpointSlotsComponent> ent, ref ComponentInit args)
    {
        EnsureSlots(ent.Owner, ent.Comp);
    }

    private void OnSlotsMapInit(Entity<RMCHardpointSlotsComponent> ent, ref MapInitEvent args)
    {
        EnsureSlots(ent.Owner, ent.Comp);
    }

    private void OnInserted(Entity<RMCHardpointSlotsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryGetSlot(ent.Comp, args.Container.ID, out var slot))
            return;

        if (!IsValidHardpoint(args.Entity, slot))
        {
            if (TryComp<ItemSlotsComponent>(ent.Owner, out var itemSlots))
                _itemSlots.TryEject(ent.Owner, args.Container.ID, null, out _, itemSlots, excludeUserAudio: true);

            return;
        }

        RefreshCanRun(ent.Owner);
    }

    private void OnRemoved(Entity<RMCHardpointSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryGetSlot(ent.Comp, args.Container.ID, out _))
            return;

        RefreshCanRun(ent.Owner);
    }

    private void OnInsertAttempt(Entity<RMCHardpointSlotsComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (!TryGetSlot(ent.Comp, args.Slot.ID, out var slot))
            return;

        if (ent.Comp.CompletingInserts.Contains(slot.Id))
            return;

        if (slot.InsertDelay <= 0f)
            return;

        if (!ent.Comp.PendingInserts.Add(slot.Id))
        {
            args.Cancelled = true;
            return;
        }

        args.Cancelled = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User.Value, slot.InsertDelay, new RMCHardpointInsertDoAfterEvent(slot.Id), ent.Owner, ent.Owner, args.Item)
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
            ent.Comp.PendingInserts.Remove(slot.Id);
    }

    private void OnVehicleCanRun(Entity<RMCHardpointSlotsComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun || HasAllRequired(ent.Owner, ent.Comp))
            return;

        args.CanRun = false;
    }

    private void OnInsertDoAfter(Entity<RMCHardpointSlotsComponent> ent, ref RMCHardpointInsertDoAfterEvent args)
    {
        ent.Comp.PendingInserts.Remove(args.SlotId);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } item || string.IsNullOrEmpty(args.SlotId))
            return;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return;

        if (!TryGetSlot(ent.Comp, args.SlotId, out var hardpointSlot))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, args.SlotId, out var slot, itemSlots))
            return;

        if (!IsValidHardpoint(item, hardpointSlot))
            return;

        ent.Comp.CompletingInserts.Add(args.SlotId);
        _itemSlots.TryInsertFromHand(ent.Owner, slot, args.User, excludeUserAudio: false);
        ent.Comp.CompletingInserts.Remove(args.SlotId);
    }

    private void EnsureSlots(EntityUid uid, RMCHardpointSlotsComponent component, ItemSlotsComponent? itemSlots = null)
    {
        if (component.Slots.Count == 0)
            return;

        itemSlots ??= EnsureComp<ItemSlotsComponent>(uid);

        foreach (var slot in component.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (_itemSlots.TryGetSlot(uid, slot.Id, out _, itemSlots))
                continue;

            var whitelist = slot.Whitelist ?? new EntityWhitelist();

            if (whitelist.Components == null || whitelist.Components.Length == 0)
                whitelist.Components = new[] { RMCHardpointItemComponent.ComponentId };

            var itemSlot = new ItemSlot
            {
                Whitelist = whitelist,
            };

            _itemSlots.AddItemSlot(uid, slot.Id, itemSlot, itemSlots);
        }
    }

    private bool TryGetSlot(RMCHardpointSlotsComponent component, string? id, [NotNullWhen(true)] out RMCHardpointSlot? slot)
    {
        slot = null;

        if (id == null)
            return false;

        foreach (var hardpoint in component.Slots)
        {
            if (hardpoint.Id == id)
            {
                slot = hardpoint;
                return true;
            }
        }

        return false;
    }

    private bool IsValidHardpoint(EntityUid item, RMCHardpointSlot slot)
    {
        if (!TryComp<RMCHardpointItemComponent>(item, out var hardpoint))
            return false;

        if (string.IsNullOrWhiteSpace(slot.HardpointType))
            return true;

        return string.Equals(hardpoint.HardpointType, slot.HardpointType, StringComparison.OrdinalIgnoreCase);
    }

    private bool HasAllRequired(EntityUid uid, RMCHardpointSlotsComponent component, ItemSlotsComponent? itemSlots = null)
    {
        if (component.Slots.Count == 0)
            return true;

        if (!Resolve(uid, ref itemSlots, logMissing: false))
            return true;

        foreach (var slot in component.Slots)
        {
            if (!slot.Required)
                continue;

            if (!_itemSlots.TryGetSlot(uid, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                return false;
        }

        return true;
    }

    private void RefreshCanRun(EntityUid uid)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicle))
            return;

        _vehicles.RefreshCanRun((uid, vehicle));
    }
}
