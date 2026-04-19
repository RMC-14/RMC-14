using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Tools;
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
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Marines.Skills;

namespace Content.Shared._RMC14.Vehicle;

public sealed class HardpointSystem : EntitySystem
{
    private static readonly EntProtoId<SkillDefinitionComponent> EngineerSkill = "RMCSkillEngineer";

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly Content.Shared.Vehicle.VehicleSystem _vehicles = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly VehicleWheelSystem _wheels = default!;
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
    [Dependency] private readonly VehicleTopologySystem _topology = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HardpointSlotsComponent, ComponentInit>(OnSlotsInit);
        SubscribeLocalEvent<HardpointSlotsComponent, MapInitEvent>(OnSlotsMapInit);
        SubscribeLocalEvent<HardpointSlotsComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<HardpointSlotsComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<HardpointSlotsComponent, VehicleCanRunEvent>(OnVehicleCanRun);
        SubscribeLocalEvent<HardpointSlotsComponent, DamageModifyEvent>(OnVehicleDamageModify);
        SubscribeLocalEvent<HardpointIntegrityComponent, ComponentInit>(OnHardpointIntegrityInit);
        SubscribeLocalEvent<HardpointIntegrityComponent, InteractUsingEvent>(
            OnHardpointRepair,
            before: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<HardpointIntegrityComponent, ExaminedEvent>(OnHardpointExamined);
        SubscribeLocalEvent<HardpointIntegrityComponent, HardpointRepairDoAfterEvent>(OnHardpointRepairDoAfter);
    }

    private void OnSlotsInit(Entity<HardpointSlotsComponent> ent, ref ComponentInit args)
    {
        EnsureSlots(ent.Owner, ent.Comp);
    }

    private void OnSlotsMapInit(Entity<HardpointSlotsComponent> ent, ref MapInitEvent args)
    {
        EnsureSlots(ent.Owner, ent.Comp);
    }

    private void OnInserted(Entity<HardpointSlotsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryGetSlot(ent.Comp, args.Container.ID, out var slot))
            return;

        ent.Comp.PendingRemovals.Clear();

        if (!IsValidHardpoint(args.Entity, ent.Comp, slot))
        {
            if (TryComp<ItemSlotsComponent>(ent.Owner, out var itemSlots))
                _itemSlots.TryEject(ent.Owner, args.Container.ID, null, out _, itemSlots, excludeUserAudio: true);

            return;
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

    private void OnRemoved(Entity<HardpointSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryGetSlot(ent.Comp, args.Container.ID, out _))
            return;

        ApplyArmorHardpointModifiers(ent.Owner, args.Entity, adding: false);
        RefreshSupportModifiers(ent.Owner);

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

        if (!TryComp(vehicle, out HardpointSlotsComponent? hardpoints) ||
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
        var accelMult = 1f;
        var viewScale = 0f;
        var cursorMaxOffset = 0f;
        var cursorOffsetSpeed = 0.5f;
        var cursorPvsIncrease = 0f;
        var hasWeaponMods = false;
        var hasSpeedMods = false;
        var hasAccelMods = false;
        var hasViewMods = false;

        void Accumulate(EntityUid item)
        {
            if (TryComp(item, out VehicleWeaponSupportAttachmentComponent? weaponMod))
            {
                accuracyMult *= weaponMod.AccuracyMultiplier;
                fireRateMult *= weaponMod.FireRateMultiplier;
                hasWeaponMods = true;
            }

            if (TryComp(item, out VehicleSpeedModifierAttachmentComponent? speedMod))
            {
                speedMult *= speedMod.SpeedMultiplier;
                hasSpeedMods = true;
            }

            if (TryComp(item, out VehicleAccelerationModifierAttachmentComponent? accelMod))
            {
                accelMult *= accelMod.AccelerationMultiplier;
                hasAccelMods = true;
            }

            if (TryComp(item, out VehicleGunnerViewAttachmentComponent? viewMod))
            {
                viewScale = Math.Max(viewScale, viewMod.PvsScale);
                cursorMaxOffset = Math.Max(cursorMaxOffset, viewMod.CursorMaxOffset);
                cursorOffsetSpeed = MathF.Max(cursorOffsetSpeed, viewMod.CursorOffsetSpeed);
                cursorPvsIncrease = Math.Max(cursorPvsIncrease, viewMod.CursorPvsIncrease);
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

            if (!TryComp(item, out HardpointSlotsComponent? turretSlots) ||
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
            var mods = EnsureComp<VehicleWeaponSupportModifierComponent>(vehicle);
            mods.AccuracyMultiplier = accuracyMult;
            mods.FireRateMultiplier = fireRateMult;
            Dirty(vehicle, mods);
        }
        else
        {
            RemCompDeferred<VehicleWeaponSupportModifierComponent>(vehicle);
        }

        if (hasSpeedMods)
        {
            var speed = EnsureComp<VehicleSpeedModifierComponent>(vehicle);
            speed.SpeedMultiplier = speedMult;
            Dirty(vehicle, speed);
        }
        else
        {
            RemCompDeferred<VehicleSpeedModifierComponent>(vehicle);
        }

        if (hasAccelMods)
        {
            var accel = EnsureComp<VehicleAccelerationModifierComponent>(vehicle);
            accel.AccelerationMultiplier = accelMult;
            Dirty(vehicle, accel);
        }
        else
        {
            RemCompDeferred<VehicleAccelerationModifierComponent>(vehicle);
        }

        if (hasViewMods && viewScale > 0f)
        {
            var view = EnsureComp<VehicleGunnerViewComponent>(vehicle);
            view.PvsScale = viewScale;
            view.CursorMaxOffset = cursorMaxOffset;
            view.CursorOffsetSpeed = cursorOffsetSpeed;
            view.CursorPvsIncrease = cursorPvsIncrease;
            Dirty(vehicle, view);
        }
        else
        {
            RemCompDeferred<VehicleGunnerViewComponent>(vehicle);
        }

        RefreshVehicleGunModifiers(vehicle, hardpoints, itemSlots);
    }

    private void RefreshVehicleGunModifiers(EntityUid vehicle, HardpointSlotsComponent hardpoints, ItemSlotsComponent itemSlots)
    {
        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            RefreshGunModifiers(itemSlot.Item!.Value);

            if (!TryComp(itemSlot.Item.Value, out HardpointSlotsComponent? turretSlots) ||
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

        if (!TryComp(hardpointItem, out VehicleArmorHardpointComponent? armor))
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
        var ev = new HardpointSlotsChangedEvent(vehicle);
        RaiseLocalEvent(vehicle, ev, broadcast: true);
    }

    private void RaiseVehicleSlotsChanged(EntityUid owner)
    {
        if (!TryGetContainingVehicleFrame(owner, out var vehicle))
            return;

        RaiseHardpointSlotsChanged(vehicle);
    }

    private void OnVehicleCanRun(Entity<HardpointSlotsComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun || HasAllRequired(ent.Owner, ent.Comp))
            return;

        args.CanRun = false;
    }

    private void EnsureSlots(EntityUid uid, HardpointSlotsComponent component, ItemSlotsComponent? itemSlots = null)
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
                    Components = new[] { HardpointItemComponent.ComponentId },
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
                    whitelist.Components = new[] { HardpointItemComponent.ComponentId };
            }

            var itemSlot = new ItemSlot
            {
                Whitelist = whitelist,
            };

            _itemSlots.AddItemSlot(uid, slot.Id, itemSlot, itemSlots);
        }
    }

    internal bool TryGetSlot(HardpointSlotsComponent component, string? id, [NotNullWhen(true)] out HardpointSlot? slot)
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

    internal bool IsValidHardpoint(EntityUid item, HardpointSlotsComponent slots, HardpointSlot slot)
    {
        if (!TryComp<HardpointItemComponent>(item, out var hardpoint))
            return false;

        if (slots.VehicleFamily is not null)
        {
            if (hardpoint.VehicleFamily is not { } vehicleFamily)
                return false;

            if (vehicleFamily != slots.VehicleFamily.Value)
                return false;
        }

        if (slot.SlotType is not null)
        {
            if (hardpoint.SlotType is not { } slotType)
                return false;

            if (slotType != slot.SlotType.Value)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(slot.CompatibilityId))
        {
            if (string.IsNullOrWhiteSpace(hardpoint.CompatibilityId))
                return false;

            if (!string.Equals(hardpoint.CompatibilityId, slot.CompatibilityId, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (string.IsNullOrWhiteSpace(slot.HardpointType))
            return slot.Whitelist == null || _whitelist.IsValid(slot.Whitelist, item);

        if (!string.Equals(hardpoint.HardpointType, slot.HardpointType, StringComparison.OrdinalIgnoreCase))
            return false;

        return slot.Whitelist == null || _whitelist.IsValid(slot.Whitelist, item);
    }

    private bool HasAllRequired(EntityUid uid, HardpointSlotsComponent component, ItemSlotsComponent? itemSlots = null)
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

            if (itemSlot.Item is { } item && TryComp(item, out HardpointIntegrityComponent? integrity) && integrity.Integrity <= 0f)
                return false;
        }

        return true;
    }

    internal void RefreshCanRun(EntityUid uid)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicle))
            return;

        _vehicles.RefreshCanRun((uid, vehicle));
    }

    private void OnVehicleDamageModify(Entity<HardpointSlotsComponent> ent, ref DamageModifyEvent args)
    {
        if (_net.IsClient)
            return;

        var totalDamage = args.Damage.GetTotal().Float();
        if (totalDamage <= 0f)
            return;

        if (!TryComp(ent.Owner, out ItemSlotsComponent? itemSlots))
            return;

        var topLevelHardpoints = new List<(EntityUid Item, HardpointIntegrityComponent Integrity)>();
        CollectIntactTopLevelHardpoints(ent.Owner, ent.Comp, itemSlots, topLevelHardpoints);

        var anyTopLevelIntact = topLevelHardpoints.Count > 0;

        if (anyTopLevelIntact)
        {
            var visited = new HashSet<EntityUid>();
            foreach (var (item, integrity) in topLevelHardpoints)
            {
                ApplyDamageToHardpointTree(ent.Owner, item, integrity, args.Damage, visited);
            }
        }

        var hullFraction = anyTopLevelIntact ? ent.Comp.FrameDamageFractionWhileIntact : 1f;
        if (TryComp(ent.Owner, out HardpointIntegrityComponent? frameIntegrity))
        {
            var frameDamage = ScaleDamage(args.Damage, hullFraction);
            var frameAmount = GetVehicleFrameDamageAmount(ent.Owner, frameDamage);
            if (frameAmount > 0f)
                DamageHardpoint(ent.Owner, ent.Owner, frameAmount, frameIntegrity);
        }

        args.Damage = ScaleDamage(args.Damage, hullFraction);
    }

    private void CollectIntactTopLevelHardpoints(
        EntityUid owner,
        HardpointSlotsComponent slots,
        ItemSlotsComponent itemSlots,
        List<(EntityUid Item, HardpointIntegrityComponent Integrity)> intactHardpoints)
    {
        foreach (var slot in slots.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(owner, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                continue;

            if (itemSlot.Item is not { } item)
                continue;

            if (TryComp(item, out HardpointIntegrityComponent? integrity) && integrity.Integrity > 0f)
                intactHardpoints.Add((item, integrity));
        }
    }

    private void ApplyDamageToHardpointTree(
        EntityUid vehicle,
        EntityUid hardpoint,
        HardpointIntegrityComponent integrity,
        DamageSpecifier damage,
        HashSet<EntityUid> visited)
    {
        if (!visited.Add(hardpoint))
            return;

        ApplyDamageToHardpoint(vehicle, hardpoint, integrity, damage);

        if (!TryComp(hardpoint, out HardpointSlotsComponent? childSlots) ||
            !TryComp(hardpoint, out ItemSlotsComponent? childItemSlots))
        {
            return;
        }

        foreach (var slot in childSlots.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(hardpoint, slot.Id, out var itemSlot, childItemSlots) ||
                itemSlot.Item is not { } childHardpoint)
            {
                continue;
            }

            if (!TryComp(childHardpoint, out HardpointIntegrityComponent? childIntegrity) ||
                childIntegrity.Integrity <= 0f)
            {
                continue;
            }

            ApplyDamageToHardpointTree(vehicle, childHardpoint, childIntegrity, damage, visited);
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

    private void ApplyDamageToHardpoint(EntityUid vehicle, EntityUid hardpoint, HardpointIntegrityComponent integrity, DamageSpecifier damage)
    {
        var amount = GetHardpointDamageAmount(hardpoint, damage);
        if (amount <= 0f)
            return;

        DamageHardpoint(vehicle, hardpoint, amount, integrity);
    }

    private float GetHardpointDamageAmount(EntityUid hardpoint, DamageSpecifier damage)
    {
        var total = MathF.Max(damage.GetTotal().Float(), 0f);
        var modifierSets = new List<DamageModifierSet>();
        CollectHardpointDamageModifierSets(hardpoint, modifierSets);

        if (modifierSets.Count > 0)
        {
            var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage, modifierSets);
            total = MathF.Max(modifiedDamage.GetTotal().Float(), 0f);
        }

        if (TryComp<HardpointItemComponent>(hardpoint, out var hardpointItem))
            total *= MathF.Max(hardpointItem.DamageMultiplier, 0f);

        return total;
    }

    private void CollectHardpointDamageModifierSets(EntityUid hardpoint, List<DamageModifierSet> modifierSets)
    {
        if (TryComp(hardpoint, out HardpointDamageModifierComponent? hardpointModifiers))
        {
            foreach (var modifierSetId in hardpointModifiers.ModifierSets)
            {
                if (_prototypeManager.TryIndex<DamageModifierSetPrototype>(modifierSetId, out var modifierSet))
                    modifierSets.Add(modifierSet);
            }
        }

        if (TryComp(hardpoint, out VehicleArmorHardpointComponent? armorHardpoint))
        {
            foreach (var modifierSetId in armorHardpoint.ModifierSets)
            {
                if (_prototypeManager.TryIndex<DamageModifierSetPrototype>(modifierSetId, out var modifierSet))
                    modifierSets.Add(modifierSet);
            }
        }
    }

    private float GetVehicleFrameDamageAmount(EntityUid vehicle, DamageSpecifier damage)
    {
        var total = MathF.Max(damage.GetTotal().Float(), 0f);
        if (!TryComp(vehicle, out DamageProtectionBuffComponent? protection) ||
            protection.Modifiers.Count == 0)
        {
            return total;
        }

        var modifiedDamage = damage;
        foreach (var modifier in protection.Modifiers.Values)
        {
            modifiedDamage = DamageSpecifier.ApplyModifierSet(modifiedDamage, modifier);
        }

        return MathF.Max(modifiedDamage.GetTotal().Float(), 0f);
    }

    private void OnHardpointIntegrityInit(Entity<HardpointIntegrityComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Integrity <= 0f)
            ent.Comp.Integrity = ent.Comp.MaxIntegrity;

        UpdateFrameDamageAppearance(ent.Owner, ent.Comp);
    }

    private void OnHardpointExamined(Entity<HardpointIntegrityComponent> ent, ref ExaminedEvent args)
    {
        var current = ent.Comp.Integrity;
        var max = ent.Comp.MaxIntegrity;
        var percent = max > 0f ? current / max : 0f;

        if (HasComp<XenoComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString(GetHardpointConditionString(percent)));
            return;
        }

        var color = GetHardpointIntegrityColor(percent);
        args.PushMarkup(Loc.GetString("rmc-hardpoint-integrity-examine",
            ("color", color),
            ("current", (int)MathF.Ceiling(current)),
            ("max", (int)MathF.Ceiling(max)),
            ("percent", (int)MathF.Round(percent * 100f))));

        if (TryGetArmorExamineModifiers(ent.Owner, out var acid, out var slash, out var bullet, out var explosive, out var blunt))
        {
            args.PushMarkup(Loc.GetString("rmc-hardpoint-armor-modifiers-examine",
                ("acid", FormatModifierValue(acid)),
                ("slash", FormatModifierValue(slash)),
                ("bullet", FormatModifierValue(bullet)),
                ("explosive", FormatModifierValue(explosive)),
                ("blunt", FormatModifierValue(blunt))));
        }
    }

    private bool TryGetArmorExamineModifiers(
        EntityUid uid,
        out float acid,
        out float slash,
        out float bullet,
        out float explosive,
        out float blunt)
    {
        acid = 1f;
        slash = 1f;
        bullet = 1f;
        explosive = 1f;
        blunt = 1f;

        if (!TryComp(uid, out VehicleArmorHardpointComponent? armor))
            return false;

        if (TryComp(uid, out HardpointItemComponent? item) &&
            item.VehicleFamily == "Tank" &&
            _prototypeManager.TryIndex<DamageModifierSetPrototype>("VehicleFrameTank", out var tankBase))
        {
            ApplyDamageModifierCoefficients(tankBase, ref acid, ref slash, ref bullet, ref explosive, ref blunt);
        }

        foreach (var modifierSetId in armor.ModifierSets)
        {
            if (!_prototypeManager.TryIndex(modifierSetId, out DamageModifierSetPrototype? modifierSet))
                continue;

            ApplyDamageModifierCoefficients(modifierSet, ref acid, ref slash, ref bullet, ref explosive, ref blunt);
        }

        return true;
    }

    private static void ApplyDamageModifierCoefficients(
        DamageModifierSet modifierSet,
        ref float acid,
        ref float slash,
        ref float bullet,
        ref float explosive,
        ref float blunt)
    {
        if (modifierSet.Coefficients.TryGetValue("Caustic", out var acidCoefficient))
            acid *= acidCoefficient;

        if (modifierSet.Coefficients.TryGetValue("Slash", out var slashCoefficient))
            slash *= slashCoefficient;

        if (modifierSet.Coefficients.TryGetValue("Piercing", out var bulletCoefficient))
            bullet *= bulletCoefficient;

        if (modifierSet.Coefficients.TryGetValue("Structural", out var explosiveCoefficient))
            explosive *= explosiveCoefficient;

        if (modifierSet.Coefficients.TryGetValue("Blunt", out var bluntCoefficient))
            blunt *= bluntCoefficient;
    }

    private static string FormatModifierValue(float value)
    {
        return value.ToString("0.###");
    }

    private string GetHardpointIntegrityColor(float percent)
    {
        if (percent >= 0.9f)
            return "green";

        if (percent >= 0.7f)
            return "yellow";

        if (percent >= 0.4f)
            return "orange";

        if (percent >= 0.15f)
            return "red";

        return "crimson";
    }

    private string GetHardpointConditionString(float percent)
    {
        if (percent >= 0.9f)
            return "rmc-hardpoint-condition-pristine";

        if (percent >= 0.7f)
            return "rmc-hardpoint-condition-good";

        if (percent >= 0.4f)
            return "rmc-hardpoint-condition-worn";

        if (percent >= 0.15f)
            return "rmc-hardpoint-condition-bad";

        return "rmc-hardpoint-condition-critical";
    }

    public bool DamageHardpoint(EntityUid vehicle, EntityUid hardpoint, float amount, HardpointIntegrityComponent? integrity = null)
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

        if (TryComp(hardpoint, out VehicleWheelItemComponent? _))
            _wheels.OnWheelDamaged(vehicle);

        if (previous > 0f && integrity.Integrity <= 0f)
            RefreshCanRun(vehicle);

        UpdateHardpointUi(vehicle);
        return true;
    }

    private void OnHardpointRepair(Entity<HardpointIntegrityComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == null)
            return;

        var used = args.Used;
        var isFrame = HasComp<HardpointSlotsComponent>(ent.Owner);
        var usedWelder = _tool.HasQuality(used, ent.Comp.RepairToolQuality) && HasComp<BlowtorchComponent>(used);
        var usedWrench = isFrame && _tool.HasQuality(used, ent.Comp.FrameFinishToolQuality);

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

        var weldCap = ent.Comp.MaxIntegrity * ent.Comp.FrameWeldCapFraction;

        if (usedWelder && isFrame && ent.Comp.Integrity >= weldCap - ent.Comp.FrameRepairEpsilon)
        {
            _popup.PopupClient("Finish tightening the frame with a wrench.", ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (usedWrench && ent.Comp.Integrity < weldCap - ent.Comp.FrameRepairEpsilon)
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

        var repairAmount = GetRepairAmountForCurrentStep(ent.Owner, ent.Comp, usedWelder, usedWrench, isFrame);
        if (repairAmount <= 0f)
        {
            args.Handled = true;
            return;
        }

        var repairTime = GetRepairTimeForCurrentStep(ent.Owner, args.User, ent.Comp, repairAmount, isFrame);

        ent.Comp.Repairing = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, repairTime, new HardpointRepairDoAfterEvent(), ent.Owner, ent.Owner, used)
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

    private void OnHardpointRepairDoAfter(Entity<HardpointIntegrityComponent> ent, ref HardpointRepairDoAfterEvent args)
    {
        ent.Comp.Repairing = false;

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var used = args.Used;
        var isFrame = HasComp<HardpointSlotsComponent>(ent.Owner);
        var usedWelder = used != null && _tool.HasQuality(used.Value, ent.Comp.RepairToolQuality) && HasComp<BlowtorchComponent>(used);
        var usedWrench = isFrame && used != null && _tool.HasQuality(used.Value, ent.Comp.FrameFinishToolQuality);

        if (!usedWelder && !usedWrench)
            return;

        if (usedWelder)
        {
            if (used == null || !_repairable.UseFuel(used.Value, args.User, ent.Comp.RepairFuelCost))
                return;
        }

        var repairAmount = GetRepairAmountForCurrentStep(ent.Owner, ent.Comp, usedWelder, usedWrench, isFrame);
        if (repairAmount <= 0f)
            return;

        ent.Comp.Integrity = MathF.Min(ent.Comp.MaxIntegrity, ent.Comp.Integrity + repairAmount);

        Dirty(ent.Owner, ent.Comp);
        UpdateFrameDamageAppearance(ent.Owner, ent.Comp);

        if (ent.Comp.RepairSound != null)
            _audio.PlayPredicted(ent.Comp.RepairSound, ent.Owner, args.User);

        _popup.PopupClient(Loc.GetString("rmc-hardpoint-repaired"), ent.Owner, args.User);

        var vehicle = ent.Owner;
        if (TryComp(ent.Owner, out VehicleWheelItemComponent? _))
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

        if (ShouldRepeatRepair(ent.Owner, ent.Comp, usedWelder, usedWrench, isFrame))
            args.Repeat = true;
    }

    private float GetRepairAmountForCurrentStep(
        EntityUid uid,
        HardpointIntegrityComponent integrity,
        bool usedWelder,
        bool usedWrench,
        bool isFrame)
    {
        if (integrity.MaxIntegrity <= 0f)
            return 0f;

        var chunkSize = MathF.Max(integrity.RepairChunkMinimum, integrity.MaxIntegrity * integrity.RepairChunkFraction);
        var weldCap = integrity.MaxIntegrity * integrity.FrameWeldCapFraction;

        if (usedWelder)
        {
            var target = isFrame ? MathF.Min(weldCap, integrity.MaxIntegrity) : integrity.MaxIntegrity;
            return MathF.Max(0f, MathF.Min(chunkSize, target - integrity.Integrity));
        }

        if (usedWrench)
            return MathF.Max(0f, MathF.Min(chunkSize, integrity.MaxIntegrity - integrity.Integrity));

        return 0f;
    }

    private float GetRepairTimeForCurrentStep(
        EntityUid uid,
        EntityUid user,
        HardpointIntegrityComponent integrity,
        float repairAmount,
        bool isFrame)
    {
        if (integrity.MaxIntegrity <= 0f || repairAmount <= 0f)
            return 0f;

        var repairFraction = repairAmount / integrity.MaxIntegrity;
        var skillMultiplier = _skills.GetSkillDelayMultiplier(user, EngineerSkill);

        if (isFrame)
            return integrity.FrameRepairChunkSeconds * (repairFraction / integrity.RepairChunkFraction) * skillMultiplier;

        var repairRate = GetHardpointRepairRate(uid);
        return (repairFraction / repairRate) * skillMultiplier;
    }

    private float GetHardpointRepairRate(EntityUid uid)
    {
        if (TryComp(uid, out HardpointItemComponent? hardpoint))
            return hardpoint.RepairRate > 0f ? hardpoint.RepairRate : 0.01f;

        return 0.01f;
    }

    private bool ShouldRepeatRepair(
        EntityUid uid,
        HardpointIntegrityComponent integrity,
        bool usedWelder,
        bool usedWrench,
        bool isFrame)
    {
        if (integrity.Integrity >= integrity.MaxIntegrity)
            return false;

        if (isFrame)
        {
            var weldCap = integrity.MaxIntegrity * integrity.FrameWeldCapFraction;

            if (usedWelder)
                return integrity.Integrity < weldCap - integrity.FrameRepairEpsilon;

            if (usedWrench)
                return integrity.Integrity >= weldCap - integrity.FrameRepairEpsilon &&
                       integrity.Integrity < integrity.MaxIntegrity;

            return false;
        }

        return usedWelder && integrity.Integrity > 0f && integrity.Integrity < integrity.MaxIntegrity;
    }

    private EntityUid? GetVehicleFromPart(EntityUid part)
    {
        if (!_containers.TryGetContainingContainer(part, out var container))
            return null;

        return container.Owner;
    }

    internal void UpdateHardpointUi(EntityUid uid, HardpointSlotsComponent? component = null, ItemSlotsComponent? itemSlots = null)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(uid, ref component, logMissing: false))
            return;

        if (!Resolve(uid, ref itemSlots, logMissing: false))
            return;

        var entries = new List<HardpointUiEntry>(component.Slots.Count);
        float frameIntegrity = 0f;
        float frameMaxIntegrity = 0f;
        var hasFrameIntegrity = false;

        if (TryComp(uid, out HardpointIntegrityComponent? frame))
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

                if (TryComp(item, out HardpointIntegrityComponent? hardpointIntegrity))
                {
                    integrity = hardpointIntegrity.Integrity;
                    maxIntegrity = hardpointIntegrity.MaxIntegrity;
                    hasIntegrity = true;
                }
            }

            entries.Add(new HardpointUiEntry(
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
                TryComp(turretItem, out HardpointSlotsComponent? turretSlots) &&
                TryComp(turretItem, out ItemSlotsComponent? turretItemSlots))
            {
                AppendTurretEntries(entries, slot.Id, turretItem, turretSlots, turretItemSlots);
            }
        }

        _ui.SetUiState(uid,
            HardpointUiKey.Key,
            new HardpointBoundUserInterfaceState(
                entries,
                frameIntegrity,
                frameMaxIntegrity,
                hasFrameIntegrity,
                component.LastUiError));
    }

    internal bool HasAttachedHardpoints(EntityUid owner, HardpointSlotsComponent slots, ItemSlotsComponent itemSlots)
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
        List<HardpointUiEntry> entries,
        string parentSlotId,
        EntityUid turretUid,
        HardpointSlotsComponent turretSlots,
        ItemSlotsComponent turretItemSlots)
    {
        foreach (var turretSlot in turretSlots.Slots)
        {
            if (string.IsNullOrWhiteSpace(turretSlot.Id))
                continue;

            var compositeId = VehicleTurretSlotIds.Compose(parentSlotId, turretSlot.Id);
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

                if (TryComp(installedItem, out HardpointIntegrityComponent? hardpointIntegrity))
                {
                    integrity = hardpointIntegrity.Integrity;
                    maxIntegrity = hardpointIntegrity.MaxIntegrity;
                    hasIntegrity = true;
                }
            }

            entries.Add(new HardpointUiEntry(
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

    internal void UpdateContainingVehicleUi(EntityUid owner)
    {
        if (!TryGetContainingVehicleFrame(owner, out var vehicle))
            return;

        UpdateHardpointUi(vehicle);
    }

    internal void SetContainingVehicleUiError(EntityUid owner, string? error)
    {
        if (!TryGetContainingVehicleFrame(owner, out var vehicle))
            return;

        if (!TryComp(vehicle, out HardpointSlotsComponent? slots))
            return;

        slots.LastUiError = error;
    }

    internal bool TryGetContainingVehicleFrame(EntityUid owner, out EntityUid vehicle)
    {
        return _topology.TryGetVehicle(owner, out vehicle);
    }

    private void UpdateFrameDamageAppearance(EntityUid uid, HardpointIntegrityComponent component)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        var max = component.MaxIntegrity > 0f ? component.MaxIntegrity : 1f;
        var fraction = Math.Clamp(max > 0f ? component.Integrity / max : 1f, 0f, 1f);

        _appearance.SetData(uid, VehicleFrameDamageVisuals.IntegrityFraction, fraction, appearance);
    }

    internal bool TryGetPryingTool(EntityUid user, ProtoId<ToolQualityPrototype> quality, out EntityUid tool)
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

        if (!_tool.HasQuality(held.Value, quality, toolComp))
            return false;

        tool = held.Value;
        return true;
    }
}
