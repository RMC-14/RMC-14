using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Content.Shared.Tools.Systems;
using Content.Shared.Damage;
using Content.Shared._RMC14.Repairable;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.UserInterface;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCHardpointSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly VehicleSystem _vehicles = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly RMCVehicleWheelSystem _wheels = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCRepairableSystem _repairable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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
        SubscribeLocalEvent<RMCHardpointSlotsComponent, GetVerbsEvent<InteractionVerb>>(OnGetRemoveVerbs);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, DamageModifyEvent>(OnVehicleDamageModify);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, InteractUsingEvent>(OnSlotsInteractUsing);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, BoundUIOpenedEvent>(OnHardpointUiOpened);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, BoundUIClosedEvent>(OnHardpointUiClosed);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, RMCHardpointRemoveMessage>(OnHardpointRemoveMessage);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, RMCHardpointRemoveDoAfterEvent>(OnHardpointRemoveDoAfter);
        SubscribeLocalEvent<RMCHardpointIntegrityComponent, ComponentInit>(OnHardpointIntegrityInit);
        SubscribeLocalEvent<RMCHardpointIntegrityComponent, InteractUsingEvent>(OnHardpointRepair);
        SubscribeLocalEvent<RMCHardpointIntegrityComponent, ExaminedEvent>(OnHardpointExamined);
        SubscribeLocalEvent<RMCHardpointIntegrityComponent, RMCHardpointRepairDoAfterEvent>(OnHardpointRepairDoAfter);
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

        ent.Comp.PendingRemovals.Clear();

        if (!IsValidHardpoint(args.Entity, slot))
        {
            if (TryComp<ItemSlotsComponent>(ent.Owner, out var itemSlots))
                _itemSlots.TryEject(ent.Owner, args.Container.ID, null, out _, itemSlots, excludeUserAudio: true);

            return;
        }

        RefreshCanRun(ent.Owner);
        UpdateHardpointUi(ent.Owner, ent.Comp);
    }

    private void OnRemoved(Entity<RMCHardpointSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryGetSlot(ent.Comp, args.Container.ID, out _))
            return;

        RefreshCanRun(ent.Owner);
        ent.Comp.PendingRemovals.Remove(args.Container.ID);
        UpdateHardpointUi(ent.Owner, ent.Comp);
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

            if (itemSlot.Item is { } item && TryComp(item, out RMCHardpointIntegrityComponent? integrity) && integrity.Integrity <= 0f)
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

    private void OnGetRemoveVerbs(Entity<RMCHardpointSlotsComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!_tool.HasQuality(args.Using.Value, "Prying"))
            return;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return;

        foreach (var slot in ent.Comp.Slots)
        {
            if (!_itemSlots.TryGetSlot(ent.Owner, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;
            var user = args.User;
            var slotId = slot.Id;
            var verb = new InteractionVerb
            {
                Act = () => TryStartHardpointRemoval(ent.Owner, ent.Comp, user, slotId),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("rmc-hardpoint-remove-verb", ("slot", Name(itemSlot.Item!.Value))),
                Priority = itemSlot.Priority,
                IconEntity = GetNetEntity(itemSlot.Item),
            };

            args.Verbs.Add(verb);
        }
    }

    private void OnSlotsInteractUsing(Entity<RMCHardpointSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == null)
            return;

        if (!_tool.HasQuality(args.Used, "Prying"))
            return;

        if (_ui.TryOpenUi(ent.Owner, RMCHardpointUiKey.Key, args.User))
        {
            UpdateHardpointUi(ent.Owner, ent.Comp);
            args.Handled = true;
        }
    }

    private void OnHardpointUiOpened(Entity<RMCHardpointSlotsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, RMCHardpointUiKey.Key))
            return;

        UpdateHardpointUi(ent.Owner, ent.Comp);
    }

    private void OnHardpointUiClosed(Entity<RMCHardpointSlotsComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, RMCHardpointUiKey.Key))
            return;

        // Clear any pending operations when UI closes
        ent.Comp.PendingRemovals.Clear();
    }

    private void OnHardpointRemoveMessage(Entity<RMCHardpointSlotsComponent> ent, ref RMCHardpointRemoveMessage args)
    {
        if (!Equals(args.UiKey, RMCHardpointUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        TryStartHardpointRemoval(ent.Owner, ent.Comp, args.Actor, args.SlotId);
    }

    private void OnHardpointRemoveDoAfter(Entity<RMCHardpointSlotsComponent> ent, ref RMCHardpointRemoveDoAfterEvent args)
    {
        ent.Comp.PendingRemovals.Remove(args.SlotId);

        if (args.Cancelled || args.Handled)
        {
            UpdateHardpointUi(ent.Owner, ent.Comp);
            return;
        }

        args.Handled = true;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
        {
            UpdateHardpointUi(ent.Owner, ent.Comp);
            return;
        }

        if (!TryGetSlot(ent.Comp, args.SlotId, out _))
        {
            UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent.Owner, args.SlotId, out var itemSlot, itemSlots) || !itemSlot.HasItem)
        {
            UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            return;
        }

        _itemSlots.TryEjectToHands(ent.Owner, itemSlot, args.User, true);
        UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
        RefreshCanRun(ent.Owner);
    }

    private void OnVehicleDamageModify(Entity<RMCHardpointSlotsComponent> ent, ref DamageModifyEvent args)
    {
        if (_net.IsClient)
            return;

        var totalDamage = args.Damage.GetTotal().Float();
        if (totalDamage <= 0f)
            return;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return;

        var intactHardpoints = new List<(EntityUid Item, RMCHardpointIntegrityComponent Integrity)>();

        foreach (var slot in ent.Comp.Slots)
        {
            if (!_itemSlots.TryGetSlot(ent.Owner, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            if (itemSlot.Item is not { } item || !TryComp(item, out RMCHardpointIntegrityComponent? integrity))
                continue;

            if (integrity.Integrity <= 0f)
                continue;

            intactHardpoints.Add((item, integrity));
        }

        var anyIntact = intactHardpoints.Count > 0;

        if (anyIntact)
        {
            var hardpointDamage = totalDamage * ent.Comp.HardpointDamageMultiplier;

            foreach (var (item, integrity) in intactHardpoints)
            {
                ApplyDamageToHardpoint(ent.Owner, item, integrity, hardpointDamage);
            }
        }

        var hullFraction = anyIntact ? ent.Comp.FrameDamageFractionWhileIntact : 1f;
        if (TryComp(ent.Owner, out RMCHardpointIntegrityComponent? frameIntegrity))
            DamageHardpoint(ent.Owner, ent.Owner, totalDamage * hullFraction, frameIntegrity);
        args.Damage = ScaleDamage(args.Damage, hullFraction);
    }

    private DamageSpecifier ScaleDamage(DamageSpecifier source, float fraction)
    {
        if (MathF.Abs(fraction - 1f) < 0.0001f)
            return source;

        var scaled = new DamageSpecifier();
        foreach (var (type, value) in source.DamageDict)
        {
            scaled.DamageDict[type] = value * fraction;
        }

        return scaled;
    }

    private void ApplyDamageToHardpoint(EntityUid vehicle, EntityUid hardpoint, RMCHardpointIntegrityComponent integrity, float amount)
    {
        DamageHardpoint(vehicle, hardpoint, amount, integrity);
    }

    private void OnHardpointIntegrityInit(Entity<RMCHardpointIntegrityComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Integrity <= 0f)
            ent.Comp.Integrity = ent.Comp.MaxIntegrity;

        UpdateFrameDamageAppearance(ent.Owner, ent.Comp);
    }

    private void OnHardpointExamined(Entity<RMCHardpointIntegrityComponent> ent, ref ExaminedEvent args)
    {
        var current = ent.Comp.Integrity;
        var max = ent.Comp.MaxIntegrity;
        var percent = max > 0f ? MathF.Round(current / max * 100f) : 0f;

        args.PushMarkup(Loc.GetString("rmc-hardpoint-integrity-examine", ("current", MathF.Round(current, 1)), ("max", MathF.Round(max, 1)), ("percent", percent)));
    }

    public bool DamageHardpoint(EntityUid vehicle, EntityUid hardpoint, float amount, RMCHardpointIntegrityComponent? integrity = null)
    {
        if (_net.IsClient || amount <= 0f)
            return false;

        if (!Resolve(hardpoint, ref integrity, logMissing: false))
            return false;

        if (integrity.Integrity <= 0f)
            return false;

        if (integrity.Integrity > integrity.MaxIntegrity && integrity.MaxIntegrity > 0f)
            integrity.Integrity = integrity.MaxIntegrity;

        var previous = integrity.Integrity;
        integrity.Integrity = MathF.Max(0f, integrity.Integrity - amount);

        if (Math.Abs(previous - integrity.Integrity) < 0.01f)
            return false;

        Dirty(hardpoint, integrity);
        UpdateFrameDamageAppearance(hardpoint, integrity);

        if (TryComp(hardpoint, out RMCVehicleWheelItemComponent? _))
            _wheels.OnWheelDamaged(vehicle);

        if (previous > 0f && integrity.Integrity <= 0f)
            RefreshCanRun(vehicle);

        UpdateHardpointUi(vehicle);
        return true;
    }

    private void OnHardpointRepair(Entity<RMCHardpointIntegrityComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == null)
            return;

        var used = args.Used;
        if (!_tool.HasQuality(used, "Welding") || !HasComp<WelderComponent>(used))
            return;

        if (ent.Comp.Integrity >= ent.Comp.MaxIntegrity)
        {
            _popup.PopupClient(Loc.GetString("rmc-hardpoint-intact"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Repairing)
        {
            args.Handled = true;
            return;
        }

        var missingIntegrity = ent.Comp.MaxIntegrity - ent.Comp.Integrity;
        var weldTime = MathF.Max(
            ent.Comp.RepairTimeMin,
            MathF.Min(ent.Comp.RepairTimeMax, missingIntegrity * ent.Comp.RepairTimePerIntegrity)
        );

        ent.Comp.Repairing = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, weldTime, new RMCHardpointRepairDoAfterEvent(), ent.Owner, ent.Owner, used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ent.Comp.Repairing = false;
            return;
        }

        args.Handled = true;
    }

    private void OnHardpointRepairDoAfter(Entity<RMCHardpointIntegrityComponent> ent, ref RMCHardpointRepairDoAfterEvent args)
    {
        ent.Comp.Repairing = false;

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used == null || !_repairable.UseFuel(args.Used.Value, args.User, ent.Comp.RepairFuelCost))
            return;

        ent.Comp.Integrity = ent.Comp.MaxIntegrity;
        Dirty(ent.Owner, ent.Comp);
        UpdateFrameDamageAppearance(ent.Owner, ent.Comp);

        if (ent.Comp.RepairSound != null)
            _audio.PlayPredicted(ent.Comp.RepairSound, ent.Owner, args.User);

        _popup.PopupClient(Loc.GetString("rmc-hardpoint-repaired"), ent.Owner, args.User);

        var vehicle = ent.Owner;
        if (TryComp(ent.Owner, out RMCVehicleWheelItemComponent? _))
        {
            vehicle = GetVehicleFromPart(ent.Owner) ?? ent.Owner;
            _wheels.OnWheelDamaged(vehicle);
        }
        else
        {
            RefreshCanRun(ent.Owner);
        }

        if (ent.Comp.BypassEntryOnZero)
            RefreshCanRun(vehicle);

        UpdateHardpointUi(vehicle);
    }

    private EntityUid? GetVehicleFromPart(EntityUid part)
    {
        if (!_containers.TryGetContainingContainer(part, out var container))
            return null;

        return container.Owner;
    }

    private void TryStartHardpointRemoval(EntityUid uid, RMCHardpointSlotsComponent component, EntityUid user, string? slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return;

        if (!TryComp(uid, out ItemSlotsComponent? itemSlots))
        {
            UpdateHardpointUi(uid, component);
            return;
        }

        if (!TryGetSlot(component, slotId, out var slot))
        {
            UpdateHardpointUi(uid, component, itemSlots);
            return;
        }

        if (!_itemSlots.TryGetSlot(uid, slotId, out var itemSlot, itemSlots) || !itemSlot.HasItem)
        {
            UpdateHardpointUi(uid, component, itemSlots);
            return;
        }

        if (component.PendingInserts.Contains(slotId) || component.CompletingInserts.Contains(slotId))
        {
            _popup.PopupEntity("Finish installing that hardpoint before removing it.", user, user);
            return;
        }

        if (!TryGetPryingTool(user, out var tool))
        {
            _popup.PopupEntity("You need a prying tool to remove this hardpoint.", user, user);
            UpdateHardpointUi(uid, component, itemSlots);
            return;
        }

        if (!component.PendingRemovals.Add(slotId))
            return;

        var delay = slot.RemoveDelay > 0f ? slot.RemoveDelay : slot.InsertDelay;
        var doAfter = new DoAfterArgs(EntityManager, user, delay, new RMCHardpointRemoveDoAfterEvent(slotId), uid, uid, tool)
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
            component.PendingRemovals.Remove(slotId);

        UpdateHardpointUi(uid, component, itemSlots);
    }

    private void UpdateHardpointUi(EntityUid uid, RMCHardpointSlotsComponent? component = null, ItemSlotsComponent? itemSlots = null)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(uid, ref component, logMissing: false))
            return;

        if (!Resolve(uid, ref itemSlots, logMissing: false))
            return;

        var entries = new List<RMCHardpointUiEntry>(component.Slots.Count);
        float frameIntegrity = 0f;
        float frameMaxIntegrity = 0f;
        var hasFrameIntegrity = false;

        if (TryComp(uid, out RMCHardpointIntegrityComponent? frame))
        {
            frameIntegrity = frame.Integrity;
            frameMaxIntegrity = frame.MaxIntegrity;
            hasFrameIntegrity = true;
        }

        foreach (var slot in component.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            var hasItem = _itemSlots.TryGetSlot(uid, slot.Id, out var itemSlot, itemSlots) && itemSlot.HasItem;
            string? installedName = null;
            NetEntity? installedEntity = null;
            float integrity = 0f;
            float maxIntegrity = 0f;
            var hasIntegrity = false;

            if (hasItem && itemSlot!.Item is { } item)
            {
                installedEntity = GetNetEntity(item);
                installedName = Name(item);

                if (TryComp(item, out RMCHardpointIntegrityComponent? hardpointIntegrity))
                {
                    integrity = hardpointIntegrity.Integrity;
                    maxIntegrity = hardpointIntegrity.MaxIntegrity;
                    hasIntegrity = true;
                }
            }

            entries.Add(new RMCHardpointUiEntry(
                slot.Id,
                slot.HardpointType,
                installedName,
                installedEntity,
                integrity,
                maxIntegrity,
                hasIntegrity,
                hasItem,
                slot.Required,
                component.PendingRemovals.Contains(slot.Id)));
        }

        _ui.SetUiState(uid, RMCHardpointUiKey.Key, new RMCHardpointBoundUserInterfaceState(entries, frameIntegrity, frameMaxIntegrity, hasFrameIntegrity));
    }

    private void UpdateFrameDamageAppearance(EntityUid uid, RMCHardpointIntegrityComponent component)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        var max = component.MaxIntegrity > 0f ? component.MaxIntegrity : 1f;
        var fraction = Math.Clamp(max > 0f ? component.Integrity / max : 1f, 0f, 1f);

        _appearance.SetData(uid, RMCVehicleFrameDamageVisuals.IntegrityFraction, fraction, appearance);
    }

   private bool TryGetPryingTool(EntityUid user, out EntityUid tool)
    {
        tool = default;

        if (!TryComp(user, out HandsComponent? hands))
            return false;

        var activeHand = _hands.GetActiveHand((user, hands));
        if (activeHand == null)
            return false;

        if (!_hands.TryGetHeldItem((user, hands), activeHand, out var held))
            return false;

        if (!_tool.HasQuality(held.Value, "Prying"))
            return false;

        tool = held.Value;
        return true;
    }
}
