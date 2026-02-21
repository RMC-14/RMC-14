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
using Content.Shared._RMC14.Tools;
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
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion.Components;
using Robust.Shared.Utility;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCHardpointSystem : EntitySystem
{
    private const float FrameWeldCapFraction = 0.75f;
    private const float FrameRepairEpsilon = 0.01f;

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
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedGunSystem _guns = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

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
        SubscribeLocalEvent<RMCHardpointSlotsComponent, InteractUsingEvent>(OnSlotsInteractUsing, before: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<RMCHardpointSlotsComponent, BoundUIOpenedEvent>(OnHardpointUiOpened);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, BoundUIClosedEvent>(OnHardpointUiClosed);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, RMCHardpointRemoveMessage>(OnHardpointRemoveMessage);
        SubscribeLocalEvent<RMCHardpointSlotsComponent, RMCHardpointRemoveDoAfterEvent>(OnHardpointRemoveDoAfter);
        SubscribeLocalEvent<RMCHardpointIntegrityComponent, ComponentInit>(OnHardpointIntegrityInit);
        SubscribeLocalEvent<RMCHardpointIntegrityComponent, InteractUsingEvent>(
            OnHardpointRepair,
            before: new[] { typeof(ItemSlotsSystem) });
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

        var isAttachment = HasComp<VehicleTurretAttachmentComponent>(args.Entity);
        var logAttachment = isAttachment ||
                            slot.HardpointType.Equals("TurretWeapon", StringComparison.OrdinalIgnoreCase);
        var itemType = TryComp<RMCHardpointItemComponent>(args.Entity, out var hardpointItem)
            ? hardpointItem.HardpointType
            : "<none>";

        if (!IsValidHardpoint(args.Entity, slot))
        {
            if (logAttachment)
            {
                Log.Info($"[rmc-hardpoints] Insert rejected: owner={ToPrettyString(ent.Owner)} slot={slot.Id} slotType={slot.HardpointType} item={ToPrettyString(args.Entity)} itemType={itemType} attachment={isAttachment}");
            }

            if (TryComp<ItemSlotsComponent>(ent.Owner, out var itemSlots))
                _itemSlots.TryEject(ent.Owner, args.Container.ID, null, out _, itemSlots, excludeUserAudio: true);

            return;
        }

        if (logAttachment)
        {
            Log.Info($"[rmc-hardpoints] Inserted: owner={ToPrettyString(ent.Owner)} slot={slot.Id} slotType={slot.HardpointType} item={ToPrettyString(args.Entity)} itemType={itemType} attachment={isAttachment}");
        }

        ent.Comp.LastUiError = null;

        if (TryComp(args.Entity, out GunComponent? gun))
            _guns.RefreshModifiers((args.Entity, gun));

        ApplyArmorHardpointModifiers(ent.Owner, args.Entity, adding: true);
        RefreshSupportModifiers(ent.Owner);

        RefreshCanRun(ent.Owner);
        UpdateHardpointUi(ent.Owner, ent.Comp);
        UpdateContainingVehicleUi(ent.Owner);
        RaiseHardpointSlotsChanged(ent.Owner);
        RaiseVehicleSlotsChanged(ent.Owner);
    }

    private void OnRemoved(Entity<RMCHardpointSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryGetSlot(ent.Comp, args.Container.ID, out _))
            return;

        ApplyArmorHardpointModifiers(ent.Owner, args.Entity, adding: false);
        RefreshSupportModifiers(ent.Owner);

        var isAttachment = HasComp<VehicleTurretAttachmentComponent>(args.Entity);
        if (isAttachment)
        {
            Log.Info($"[rmc-hardpoints] Removed: owner={ToPrettyString(ent.Owner)} slot={args.Container.ID} item={ToPrettyString(args.Entity)} attachment=true stack={Environment.StackTrace}");
        }

        ent.Comp.LastUiError = null;
        RefreshCanRun(ent.Owner);
        ent.Comp.PendingRemovals.Remove(args.Container.ID);
        UpdateHardpointUi(ent.Owner, ent.Comp);
        UpdateContainingVehicleUi(ent.Owner);
        RaiseHardpointSlotsChanged(ent.Owner);
        RaiseVehicleSlotsChanged(ent.Owner);
    }

    private void RefreshSupportModifiers(EntityUid owner)
    {
        var vehicle = owner;
        if (!HasComp<VehicleComponent>(vehicle) && !TryGetContainingVehicleFrame(owner, out vehicle))
            return;

        if (!TryComp(vehicle, out RMCHardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots))
        {
            return;
        }

        if (_net.IsClient)
        {
            RefreshVehicleGunModifiers(vehicle, hardpoints, itemSlots);
            return;
        }

        var accuracyMult = FixedPoint2.New(1);
        var fireRateMult = 1f;
        var speedMult = 1f;
        var viewScale = 0f;
        var hasWeaponMods = false;
        var hasSpeedMods = false;
        var hasViewMods = false;

        void Accumulate(EntityUid item)
        {
            if (TryComp(item, out RMCVehicleWeaponSupportAttachmentComponent? weaponMod))
            {
                accuracyMult *= weaponMod.AccuracyMultiplier;
                fireRateMult *= weaponMod.FireRateMultiplier;
                hasWeaponMods = true;
            }

            if (TryComp(item, out RMCVehicleSpeedModifierAttachmentComponent? speedMod))
            {
                speedMult *= speedMod.SpeedMultiplier;
                hasSpeedMods = true;
            }

            if (TryComp(item, out RMCVehicleGunnerViewAttachmentComponent? viewMod))
            {
                viewScale = Math.Max(viewScale, viewMod.PvsScale);
                hasViewMods = true;
            }
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            var item = itemSlot.Item!.Value;
            Accumulate(item);

            if (!TryComp(item, out RMCHardpointSlotsComponent? turretSlots) ||
                !TryComp(item, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(turretSlot.Id))
                    continue;

                if (!_itemSlots.TryGetSlot(item, turretSlot.Id, out var turretItemSlot, turretItemSlots) ||
                    !turretItemSlot.HasItem)
                {
                    continue;
                }

                Accumulate(turretItemSlot.Item!.Value);
            }
        }

        if (hasWeaponMods)
        {
            var mods = EnsureComp<RMCVehicleWeaponSupportModifierComponent>(vehicle);
            mods.AccuracyMultiplier = accuracyMult;
            mods.FireRateMultiplier = fireRateMult;
            Dirty(vehicle, mods);
        }
        else
        {
            RemCompDeferred<RMCVehicleWeaponSupportModifierComponent>(vehicle);
        }

        if (hasSpeedMods)
        {
            var speed = EnsureComp<RMCVehicleSpeedModifierComponent>(vehicle);
            speed.SpeedMultiplier = speedMult;
            Dirty(vehicle, speed);
        }
        else
        {
            RemCompDeferred<RMCVehicleSpeedModifierComponent>(vehicle);
        }

        if (hasViewMods && viewScale > 0f)
        {
            var view = EnsureComp<RMCVehicleGunnerViewComponent>(vehicle);
            view.PvsScale = viewScale;
            Dirty(vehicle, view);
        }
        else
        {
            RemCompDeferred<RMCVehicleGunnerViewComponent>(vehicle);
        }

        RefreshVehicleGunModifiers(vehicle, hardpoints, itemSlots);
    }

    private void RefreshVehicleGunModifiers(EntityUid vehicle, RMCHardpointSlotsComponent hardpoints, ItemSlotsComponent itemSlots)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            RefreshGunModifiers(itemSlot.Item!.Value);

            if (!TryComp(itemSlot.Item.Value, out RMCHardpointSlotsComponent? turretSlots) ||
                !TryComp(itemSlot.Item.Value, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (string.IsNullOrWhiteSpace(turretSlot.Id))
                    continue;

                if (_itemSlots.TryGetSlot(itemSlot.Item.Value, turretSlot.Id, out var turretItemSlot, turretItemSlots) &&
                    turretItemSlot.HasItem)
                {
                    RefreshGunModifiers(turretItemSlot.Item!.Value);
                }
            }
        }
    }

    private void RefreshGunModifiers(EntityUid item)
    {
        if (TryComp(item, out GunComponent? gun))
            _guns.RefreshModifiers((item, gun));
    }

    private void ApplyArmorHardpointModifiers(EntityUid vehicle, EntityUid hardpointItem, bool adding)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(hardpointItem, out RMCVehicleArmorHardpointComponent? armor))
            return;

        if (armor.ModifierSets.Count > 0)
        {
            var buff = EnsureComp<DamageProtectionBuffComponent>(vehicle);

            foreach (var setId in armor.ModifierSets)
            {
                if (!_prototypeManager.TryIndex(setId, out var modifier))
                    continue;

                if (adding)
                {
                    buff.Modifiers[setId] = modifier;
                }
                else
                {
                    buff.Modifiers.Remove(setId);
                }
            }

            if (!adding && buff.Modifiers.Count == 0)
            {
                RemComp<DamageProtectionBuffComponent>(vehicle);
            }
            else
            {
                Dirty(vehicle, buff);
            }
        }

        if (armor.ExplosionCoefficient != null)
        {
            if (adding)
            {
                _explosion.SetExplosionResistance(vehicle, armor.ExplosionCoefficient.Value, worn: false);
            }
            else if (TryComp(vehicle, out ExplosionResistanceComponent? resistance) &&
                     MathF.Abs(resistance.DamageCoefficient - armor.ExplosionCoefficient.Value) < 0.0001f)
            {
                RemComp<ExplosionResistanceComponent>(vehicle);
            }
        }
    }

    private void RaiseHardpointSlotsChanged(EntityUid vehicle)
    {
        var ev = new RMCHardpointSlotsChangedEvent(vehicle);
        RaiseLocalEvent(vehicle, ev, broadcast: true);
    }

    private void RaiseVehicleSlotsChanged(EntityUid owner)
    {
        if (!TryGetContainingVehicleFrame(owner, out var vehicle))
            return;

        RaiseHardpointSlotsChanged(vehicle);
    }

    private void OnInsertAttempt(Entity<RMCHardpointSlotsComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (!TryGetSlot(ent.Comp, args.Slot.ID, out var slot))
            return;

        if (ent.Comp.CompletingInserts.Contains(slot.Id))
            return;

        if (!IsValidHardpoint(args.Item, slot))
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
        {
            ent.Comp.PendingInserts.Remove(slot.Id);
            ent.Comp.PendingInsertUsers.Remove(args.User.Value);
        }
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
        ent.Comp.PendingInsertUsers.Remove(args.User);

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

            var whitelist = slot.Whitelist;
            if (whitelist == null)
            {
                whitelist = new EntityWhitelist
                {
                    Components = new[] { RMCHardpointItemComponent.ComponentId },
                };
            }
            else
            {
                var hasComponents = whitelist.Components != null && whitelist.Components.Length > 0;
                var hasTags = whitelist.Tags != null && whitelist.Tags.Count > 0;
                var hasSizes = whitelist.Sizes != null && whitelist.Sizes.Count > 0;
                var hasSkills = whitelist.Skills != null && whitelist.Skills.Count > 0;
                var hasMinMobSize = whitelist.MinMobSize != null;

                if (!hasComponents && !hasTags && !hasSizes && !hasSkills && !hasMinMobSize)
                    whitelist.Components = new[] { RMCHardpointItemComponent.ComponentId };
            }

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

        if (slot.Whitelist != null)
            return _whitelist.IsValid(slot.Whitelist, item);

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
            if (HasComp<RMCHardpointNoRemoveComponent>(itemSlot.Item!.Value))
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

        AddTurretRemoveVerbs(ent, ref args, itemSlots);
    }

    private void OnSlotsInteractUsing(Entity<RMCHardpointSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == null)
            return;

        if (TryInsertTurretAttachment(ent, args.User, args.Used))
        {
            args.Handled = true;
            return;
        }

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

        ent.Comp.LastUiError = null;
        UpdateHardpointUi(ent.Owner, ent.Comp);
    }

    private void OnHardpointUiClosed(Entity<RMCHardpointSlotsComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, RMCHardpointUiKey.Key))
            return;

        // Clear any pending operations when UI closes
        ent.Comp.PendingRemovals.Clear();
        ent.Comp.LastUiError = null;
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
            if (args.Cancelled)
            {
                ent.Comp.LastUiError = "Hardpoint removal cancelled.";
                SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            }

            UpdateHardpointUi(ent.Owner, ent.Comp);
            UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        args.Handled = true;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
        {
            ent.Comp.LastUiError = "Unable to access hardpoint slots.";
            SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            UpdateHardpointUi(ent.Owner, ent.Comp);
            UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!TryGetSlot(ent.Comp, args.SlotId, out _))
        {
            ent.Comp.LastUiError = "That hardpoint slot is no longer available.";
            SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent.Owner, args.SlotId, out var itemSlot, itemSlots) || !itemSlot.HasItem)
        {
            ent.Comp.LastUiError = "No hardpoint is installed in that slot.";
            SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        if (!_itemSlots.TryEjectToHands(ent.Owner, itemSlot, args.User, true))
        {
            ent.Comp.LastUiError = "Couldn't remove the hardpoint. Free a hand and try again.";
            SetContainingVehicleUiError(ent.Owner, ent.Comp.LastUiError);
            UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
            UpdateContainingVehicleUi(ent.Owner);
            return;
        }

        ent.Comp.LastUiError = null;
        SetContainingVehicleUiError(ent.Owner, null);
        UpdateHardpointUi(ent.Owner, ent.Comp, itemSlots);
        UpdateContainingVehicleUi(ent.Owner);
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
        var visited = new HashSet<EntityUid>();
        CollectIntactHardpoints(ent.Owner, ent.Comp, itemSlots, intactHardpoints, visited);

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

    private void CollectIntactHardpoints(
        EntityUid owner,
        RMCHardpointSlotsComponent slots,
        ItemSlotsComponent itemSlots,
        List<(EntityUid Item, RMCHardpointIntegrityComponent Integrity)> intactHardpoints,
        HashSet<EntityUid> visited)
    {
        foreach (var slot in slots.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            if (itemSlot.Item is not { } item || !visited.Add(item))
                continue;

            if (TryComp(item, out RMCHardpointIntegrityComponent? integrity) && integrity.Integrity > 0f)
                intactHardpoints.Add((item, integrity));

            if (TryComp(item, out RMCHardpointSlotsComponent? childSlots) &&
                TryComp(item, out ItemSlotsComponent? childItemSlots))
            {
                CollectIntactHardpoints(item, childSlots, childItemSlots, intactHardpoints, visited);
            }
        }
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
        var isFrame = HasComp<RMCHardpointSlotsComponent>(ent.Owner);
        var usedWelder = _tool.HasQuality(used, "Welding") && HasComp<BlowtorchComponent>(used);
        var usedWrench = isFrame && _tool.HasQuality(used, "Anchoring");

        if (!usedWelder && !usedWrench)
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

        var weldCap = ent.Comp.MaxIntegrity * FrameWeldCapFraction;

        if (usedWelder && isFrame && ent.Comp.Integrity >= weldCap - FrameRepairEpsilon)
        {
            _popup.PopupClient("Finish tightening the frame with a wrench.", ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (usedWrench && ent.Comp.Integrity < weldCap - FrameRepairEpsilon)
        {
            _popup.PopupClient("Weld the frame before tightening it.", ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (usedWelder && !_repairable.UseFuel(used, args.User, ent.Comp.RepairFuelCost, true))
        {
            args.Handled = true;
            return;
        }

        var missingIntegrity = ent.Comp.MaxIntegrity - ent.Comp.Integrity;
        var weldTime = MathF.Max(
            ent.Comp.RepairTimeMin,
            MathF.Min(ent.Comp.RepairTimeMax, missingIntegrity * ent.Comp.RepairTimePerIntegrity)
        );
        var repairTime = usedWelder ? weldTime : ent.Comp.RepairTimeMin;

        ent.Comp.Repairing = true;

        if (used != null)
        {
            var toolEvent = new RMCToolUseEvent(args.User, TimeSpan.FromSeconds(repairTime));
            RaiseLocalEvent(used, ref toolEvent);
            if (toolEvent.Handled)
                repairTime = (float) toolEvent.Delay.TotalSeconds;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, repairTime, new RMCHardpointRepairDoAfterEvent(), ent.Owner, ent.Owner, used)
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

        var used = args.Used;
        var isFrame = HasComp<RMCHardpointSlotsComponent>(ent.Owner);
        var usedWelder = used != null && _tool.HasQuality(used.Value, "Welding") && HasComp<BlowtorchComponent>(used);
        var usedWrench = isFrame && used != null && _tool.HasQuality(used.Value, "Anchoring");

        if (!usedWelder && !usedWrench)
            return;

        if (usedWelder)
        {
            if (used == null || !_repairable.UseFuel(used.Value, args.User, ent.Comp.RepairFuelCost))
                return;
        }

        var weldCap = ent.Comp.MaxIntegrity * FrameWeldCapFraction;

        if (usedWelder)
        {
            var target = isFrame ? MathF.Min(weldCap, ent.Comp.MaxIntegrity) : ent.Comp.MaxIntegrity;
            ent.Comp.Integrity = MathF.Max(ent.Comp.Integrity, target);
        }
        else if (usedWrench)
        {
            if (ent.Comp.Integrity < weldCap - FrameRepairEpsilon)
                return;

            ent.Comp.Integrity = ent.Comp.MaxIntegrity;
        }

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

    private void TryStartHardpointRemoval(
        EntityUid uid,
        RMCHardpointSlotsComponent component,
        EntityUid user,
        string? slotId,
        EntityUid? uiOwnerUid = null,
        RMCHardpointSlotsComponent? uiOwnerComp = null)
    {
        var rootCall = uiOwnerUid == null || uiOwnerComp == null;
        uiOwnerUid ??= uid;
        uiOwnerComp ??= component;

        void RefreshUi(ItemSlotsComponent? currentItemSlots = null)
        {
            UpdateHardpointUi(uid, component, currentItemSlots);

            if (uiOwnerUid.Value != uid || !ReferenceEquals(uiOwnerComp, component))
                UpdateHardpointUi(uiOwnerUid.Value, uiOwnerComp);
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

        if (RMCVehicleTurretSlotIds.TryParse(slotId, out var parentSlotId, out var childSlotId))
        {
            if (!TryComp(uid, out ItemSlotsComponent? parentItemSlots) ||
                !TryGetSlot(component, parentSlotId, out _))
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
            if (!TryComp(turretUid, out RMCHardpointSlotsComponent? parentTurretSlots))
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

        if (!TryGetSlot(component, slotId, out var slot))
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

        if (TryComp(itemSlot.Item!.Value, out RMCHardpointSlotsComponent? attachedSlots) &&
            TryComp(itemSlot.Item!.Value, out ItemSlotsComponent? attachedItemSlots) &&
            HasAttachedHardpoints(itemSlot.Item!.Value, attachedSlots, attachedItemSlots))
        {
            const string error = "Remove the turret attachments before removing the turret.";
            _popup.PopupEntity(error, uid, user);
            SetError(error);
            RefreshUi(itemSlots);
            return;
        }

        if (HasComp<RMCHardpointNoRemoveComponent>(itemSlot.Item!.Value))
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

        if (!TryGetPryingTool(user, out var tool))
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
        {
            component.PendingRemovals.Remove(slotId);
            SetError("Couldn't start hardpoint removal.");
            RefreshUi(itemSlots);
            return;
        }

        uiOwnerComp.LastUiError = null;
        RefreshUi(itemSlots);
    }

    private bool TryInsertTurretAttachment(Entity<RMCHardpointSlotsComponent> ent, EntityUid user, EntityUid used)
    {
        if (!TryComp(used, out RMCHardpointItemComponent? hardpointItem))
            return false;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return false;

        var requiresTurret = HasComp<VehicleTurretAttachmentComponent>(used);
        var hasMatchingEmptySlot = false;

        if (requiresTurret)
        {
            Log.Info($"[rmc-hardpoints] Attachment insert attempt: user={ToPrettyString(user)} vehicle={ToPrettyString(ent.Owner)} item={ToPrettyString(used)} itemType={hardpointItem.HardpointType}");
        }

        foreach (var slot in ent.Comp.Slots)
        {
            if (!IsValidHardpoint(used, slot))
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

        var handled = false;

        foreach (var slot in ent.Comp.Slots)
        {
            if (!_itemSlots.TryGetSlot(ent.Owner, slot.Id, out var vehicleSlot, itemSlots) || !vehicleSlot.HasItem)
                continue;

            var turretUid = vehicleSlot.Item!.Value;
            if (!TryComp(turretUid, out RMCHardpointSlotsComponent? turretSlots) ||
                !TryComp(turretUid, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (!IsValidHardpoint(used, turretSlot))
                {
                    if (requiresTurret)
                    {
                        Log.Info($"[rmc-hardpoints] Attachment rejected by turret slot: turret={ToPrettyString(turretUid)} slot={turretSlot.Id} slotType={turretSlot.HardpointType} itemType={hardpointItem.HardpointType}");
                    }

                    continue;
                }

                if (!_itemSlots.TryGetSlot(turretUid, turretSlot.Id, out var turretItemSlot, turretItemSlots))
                    continue;

                if (turretItemSlot.HasItem)
                    continue;

                handled = true;
                var inserted = _itemSlots.TryInsertFromHand(turretUid, turretItemSlot, user);
                if (requiresTurret)
                {
                    Log.Info($"[rmc-hardpoints] Attachment insert result: turret={ToPrettyString(turretUid)} slot={turretSlot.Id} success={inserted}");
                }
                return true;
            }
        }

        if (requiresTurret)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-turret-no-base"), ent.Owner, user);
            return true;
        }

        return handled;
    }

    private void AddTurretRemoveVerbs(
        Entity<RMCHardpointSlotsComponent> ent,
        ref GetVerbsEvent<InteractionVerb> args,
        ItemSlotsComponent itemSlots)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!_itemSlots.TryGetSlot(ent.Owner, slot.Id, out var vehicleSlot, itemSlots) || !vehicleSlot.HasItem)
                continue;

            var turretUid = vehicleSlot.Item!.Value;
            if (!TryComp(turretUid, out RMCHardpointSlotsComponent? turretSlots) ||
                !TryComp(turretUid, out ItemSlotsComponent? turretItemSlots))
            {
                continue;
            }

            foreach (var turretSlot in turretSlots.Slots)
            {
                if (!_itemSlots.TryGetSlot(turretUid, turretSlot.Id, out var turretItemSlot, turretItemSlots) ||
                    !turretItemSlot.HasItem)
                {
                    continue;
                }

                if (HasComp<RMCHardpointNoRemoveComponent>(turretItemSlot.Item!.Value))
                    continue;

                var user = args.User;
                var slotId = turretSlot.Id;
                var verb = new InteractionVerb
                {
                    Act = () => TryStartHardpointRemoval(turretUid, turretSlots, user, slotId),
                    Category = VerbCategory.Eject,
                    Text = Loc.GetString("rmc-hardpoint-remove-verb", ("slot", Name(turretItemSlot.Item!.Value))),
                    Priority = turretItemSlot.Priority,
                    IconEntity = GetNetEntity(turretItemSlot.Item),
                };

                args.Verbs.Add(verb);
            }
        }
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

            if (hasItem && itemSlot?.Item is { } turretItem &&
                TryComp(turretItem, out RMCHardpointSlotsComponent? turretSlots) &&
                TryComp(turretItem, out ItemSlotsComponent? turretItemSlots))
            {
                AppendTurretEntries(entries, slot.Id, turretItem, turretSlots, turretItemSlots);
            }
        }

        _ui.SetUiState(uid,
            RMCHardpointUiKey.Key,
            new RMCHardpointBoundUserInterfaceState(
                entries,
                frameIntegrity,
                frameMaxIntegrity,
                hasFrameIntegrity,
                component.LastUiError));
    }

    private bool HasAttachedHardpoints(EntityUid owner, RMCHardpointSlotsComponent slots, ItemSlotsComponent itemSlots)
    {
        foreach (var slot in slots.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots) && itemSlot.HasItem)
                return true;
        }

        return false;
    }

    private void AppendTurretEntries(
        List<RMCHardpointUiEntry> entries,
        string parentSlotId,
        EntityUid turretUid,
        RMCHardpointSlotsComponent turretSlots,
        ItemSlotsComponent turretItemSlots)
    {
        foreach (var turretSlot in turretSlots.Slots)
        {
            if (string.IsNullOrWhiteSpace(turretSlot.Id))
                continue;

            var compositeId = RMCVehicleTurretSlotIds.Compose(parentSlotId, turretSlot.Id);
            var hasItem = _itemSlots.TryGetSlot(turretUid, turretSlot.Id, out var itemSlot, turretItemSlots) &&
                          itemSlot.HasItem;
            string? installedName = null;
            NetEntity? installedEntity = null;
            float integrity = 0f;
            float maxIntegrity = 0f;
            var hasIntegrity = false;

            if (hasItem && itemSlot!.Item is { } installedItem)
            {
                installedEntity = GetNetEntity(installedItem);
                installedName = Name(installedItem);

                if (TryComp(installedItem, out RMCHardpointIntegrityComponent? hardpointIntegrity))
                {
                    integrity = hardpointIntegrity.Integrity;
                    maxIntegrity = hardpointIntegrity.MaxIntegrity;
                    hasIntegrity = true;
                }
            }

            entries.Add(new RMCHardpointUiEntry(
                compositeId,
                turretSlot.HardpointType,
                installedName,
                installedEntity,
                integrity,
                maxIntegrity,
                hasIntegrity,
                hasItem,
                turretSlot.Required,
                turretSlots.PendingRemovals.Contains(turretSlot.Id)));
        }
    }

    private void UpdateContainingVehicleUi(EntityUid owner)
    {
        if (!TryGetContainingVehicleFrame(owner, out var vehicle))
            return;

        UpdateHardpointUi(vehicle);
    }

    private void SetContainingVehicleUiError(EntityUid owner, string? error)
    {
        if (!TryGetContainingVehicleFrame(owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out RMCHardpointSlotsComponent? slots))
            return;

        slots.LastUiError = error;
    }

    private bool TryGetContainingVehicleFrame(EntityUid owner, out EntityUid vehicle)
    {
        vehicle = default;
        var current = owner;

        while (_containers.TryGetContainingContainer(current, out var container))
        {
            var containerOwner = container.Owner;
            if (HasComp<VehicleComponent>(containerOwner))
            {
                vehicle = containerOwner;
                return true;
            }

            current = containerOwner;
        }

        return false;
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

        if (!TryComp(held.Value, out ToolComponent? toolComp))
            return false;

        if (!_tool.HasQuality(held.Value, "Prying", toolComp))
            return false;

        tool = held.Value;
        return true;
    }
}
