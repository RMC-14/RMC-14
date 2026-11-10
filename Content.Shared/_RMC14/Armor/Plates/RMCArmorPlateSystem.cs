using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Language;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Actions;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Armor.Plates;

public sealed class RMCArmorPlateSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCArmorPlateComponent, EntGotInsertedIntoContainerMessage>(OnPlateInserted);
        SubscribeLocalEvent<RMCArmorPlateComponent, EntGotRemovedFromContainerMessage>(OnPlateRemoved);
        SubscribeLocalEvent<RMCArmorPlateComponent, AccessoryRelayedEvent<GotEquippedEvent>>(OnPlateHolderEquipped);
        SubscribeLocalEvent<RMCArmorPlateComponent, AccessoryRelayedEvent<GotUnequippedEvent>>(OnPlateHolderUnequipped);
        SubscribeLocalEvent<RMCArmorPlateComponent, AccessoryRelayedEvent<ExaminedEvent>>(OnPlateHolderExamined);

        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, RMCArmorPlateAppliedEvent>(OnEmergencyInjectorApplied);
        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, RMCArmorPlateRemovedEvent>(OnEmergencyInjectorRemoved);
        SubscribeLocalEvent<RMCCeramicPlateActiveComponent, RMCArmorPlateAppliedEvent>(OnCeramicApplied);
        SubscribeLocalEvent<RMCAntiDecayPlateActiveComponent, RMCArmorPlateAppliedEvent>(OnAntiDecayApplied);

        SubscribeLocalEvent<RMCCoagulatorPlateActiveComponent, CMBleedAttemptEvent>(OnCoagulatorBleedAttempt);
        SubscribeLocalEvent<RMCAntiDecayPlateActiveComponent, MobStateChangedEvent>(OnAntiDecayMobStateChanged, after: [typeof(RMCUnrevivableSystem)]);
        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, RMCEmergencyInjectorInjectActionEvent>(OnEmergencyInjectorInject);
        SubscribeLocalEvent<RMCEmergencyInjectorPlateActiveComponent, RMCEmergencyInjectorToggleOverdoseActionEvent>(OnEmergencyInjectorToggleOverdose);
        SubscribeLocalEvent<RMCCeramicPlateActiveComponent, DamageModifyEvent>(OnCeramicDamageModify, after: [typeof(CMArmorSystem)]);
        SubscribeLocalEvent<RMCTranslatorPlateActiveComponent, CanUnderstandLanguageEvent>(OnTranslatorCanUnderstand);

        SubscribeLocalEvent<RMCArmorPlateComponent, ExaminedEvent>(OnPlateExamined);
    }

    private void OnPlateExamined(Entity<RMCArmorPlateComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCArmorPlateSystem)))
        {
            args.PushText("To use it, attach it to your uniform.");
            PushPlateStatus(ent, args);
        }
    }

    private void OnPlateHolderExamined(Entity<RMCArmorPlateComponent> ent, ref AccessoryRelayedEvent<ExaminedEvent> args)
    {
        using (args.Args.PushGroup(nameof(RMCArmorPlateSystem)))
        {
            args.Args.PushText($"The {Name(ent)} is attached to it.");
            PushPlateStatus(ent, args.Args);
        }
    }

    private void PushPlateStatus(EntityUid plate, ExaminedEvent args)
    {
        if (TryComp(plate, out RMCCeramicPlateComponent? ceramic))
        {
            var percentage = ceramic.MaxHealth > 0 ? (int) MathF.Floor(ceramic.Health / ceramic.MaxHealth * 100) : 0;
            var status = percentage switch
            {
                <= 0 => "It is broken.",
                < 20 => "It is crumbling apart!",
                < 50 => "It is seriously damaged.",
                < 80 => "It is moderately damaged.",
                < 100 => "It is slightly damaged.",
                _ => "It is in pristine condition.",
            };

            args.PushText(status);
        }

        if (TryComp(plate, out RMCEmergencyInjectorPlateComponent? injector))
        {
            var mode = injector.OverdoseProtection switch
            {
                RMCEmergencyInjectorOverdose.Strict => "STRICT",
                RMCEmergencyInjectorOverdose.Dynamic => "DYNAMIC",
                _ => "OFF",
            };

            args.PushText($"Overdose protection seems to be {mode}.");

            if (injector.Used)
                args.PushMarkup("[color=red]It is already used![/color]");
        }
    }

    private void OnPlateInserted(Entity<RMCArmorPlateComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryGetWearer(args.Container, out var wearer))
            return;

        ApplyPlate(wearer, ent);
        PlatePopup(wearer, ent, ent.Comp.AttachedPopup);
    }

    private void OnPlateRemoved(Entity<RMCArmorPlateComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryGetWearer(args.Container, out var wearer))
            return;

        RemovePlate(wearer, ent);
        PlatePopup(wearer, ent, ent.Comp.DetachedPopup);
    }

    private bool TryGetWearer(BaseContainer container, out EntityUid wearer)
    {
        wearer = default;
        var holder = container.Owner;
        if (!TryComp(holder, out UniformAccessoryHolderComponent? holderComp) ||
            container.ID != holderComp.ContainerId ||
            !_inventory.InSlotWithFlags(holder, SlotFlags.INNERCLOTHING))
        {
            return false;
        }

        wearer = Transform(holder).ParentUid;
        return true;
    }

    private void PlatePopup(EntityUid wearer, EntityUid plate, LocId? popup)
    {
        if (popup == null || _net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString(popup.Value, ("plate", plate)), wearer, wearer);
    }

    private void OnPlateHolderEquipped(Entity<RMCArmorPlateComponent> ent, ref AccessoryRelayedEvent<GotEquippedEvent> args)
    {
        if (_timing.ApplyingState || (args.Args.SlotFlags & SlotFlags.INNERCLOTHING) == 0)
            return;

        ApplyPlate(args.Args.Equipee, ent);
    }

    private void OnPlateHolderUnequipped(Entity<RMCArmorPlateComponent> ent, ref AccessoryRelayedEvent<GotUnequippedEvent> args)
    {
        if (_timing.ApplyingState || (args.Args.SlotFlags & SlotFlags.INNERCLOTHING) == 0)
            return;

        RemovePlate(args.Args.Equipee, ent);
        PlatePopup(args.Args.Equipee, ent, ent.Comp.UnequippedPopup);
    }

    private void ApplyPlate(EntityUid wearer, Entity<RMCArmorPlateComponent> plate)
    {
        EntityManager.AddComponents(wearer, plate.Comp.WearerComponents);

        var ev = new RMCArmorPlateAppliedEvent(plate);
        RaiseLocalEvent(wearer, ref ev);
    }

    private void RemovePlate(EntityUid wearer, Entity<RMCArmorPlateComponent> plate)
    {
        var ev = new RMCArmorPlateRemovedEvent(plate);
        RaiseLocalEvent(wearer, ref ev);

        EntityManager.RemoveComponents(wearer, plate.Comp.WearerComponents);
    }

    private void OnEmergencyInjectorApplied(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref RMCArmorPlateAppliedEvent args)
    {
        ent.Comp.Plate = args.Plate;
        _actions.AddAction(ent, ref ent.Comp.InjectActionEntity, ent.Comp.InjectAction);
        _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        Dirty(ent);
    }

    private void OnEmergencyInjectorRemoved(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref RMCArmorPlateRemovedEvent args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.InjectActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);
    }

    private void OnCeramicApplied(Entity<RMCCeramicPlateActiveComponent> ent, ref RMCArmorPlateAppliedEvent args)
    {
        ent.Comp.Plate = args.Plate;
        Dirty(ent);
    }

    private void OnAntiDecayApplied(Entity<RMCAntiDecayPlateActiveComponent> ent, ref RMCArmorPlateAppliedEvent args)
    {
        ent.Comp.Plate = args.Plate;
        Dirty(ent);
    }

    private void OnTranslatorCanUnderstand(Entity<RMCTranslatorPlateActiveComponent> ent, ref CanUnderstandLanguageEvent args)
    {
        args.CanUnderstand = true;
    }

    private void OnCoagulatorBleedAttempt(Entity<RMCCoagulatorPlateActiveComponent> ent, ref CMBleedAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnAntiDecayMobStateChanged(Entity<RMCAntiDecayPlateActiveComponent> ent, ref MobStateChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            _unrevivable.AddRevivableTime(ent.Owner, ent.Comp.BonusTime);
            ent.Comp.Preserving = true;
            Dirty(ent);

            if (ent.Comp.Plate is { } deathPlate)
                _popup.PopupEntity(Loc.GetString("rmc-armor-plate-anti-decay-death", ("plate", deathPlate)), ent, ent);

            return;
        }

        if (args.OldMobState != MobState.Dead || !ent.Comp.Preserving)
            return;

        ent.Comp.Preserving = false;
        Dirty(ent);

        if (ent.Comp.Plate is { } revivePlate)
            _popup.PopupEntity(Loc.GetString("rmc-armor-plate-anti-decay-revive", ("plate", revivePlate)), ent, ent);
    }

    private void OnEmergencyInjectorInject(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref RMCEmergencyInjectorInjectActionEvent args)
    {
        if (_net.IsClient)
            return;

        var wearer = ent.Owner;
        if (ent.Comp.Plate is not { } plateUid || !TryComp(plateUid, out RMCEmergencyInjectorPlateComponent? plate))
            return;

        if (plate.Used)
        {
            _popup.PopupEntity(Loc.GetString("rmc-armor-plate-injector-empty", ("plate", plateUid)), wearer, wearer, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetInjectableSolution(wearer, out var solutionEnt, out var solution))
            return;

        if (plate.OverdoseProtection == RMCEmergencyInjectorOverdose.Strict)
        {
            foreach (var (reagent, amount) in plate.Cocktail)
            {
                if (_rmcReagent.TryIndex(reagent, out var proto) && proto.Overdose is { } od &&
                    solution.GetTotalPrototypeQuantity(reagent) + amount > od)
                {
                    _popup.PopupEntity(Loc.GetString("rmc-armor-plate-injector-refuse"), wearer, wearer, PopupType.MediumCaution);
                    return;
                }
            }
        }

        var toInject = new Solution();
        var adjusted = false;
        var overdosed = false;
        foreach (var (reagent, amount) in plate.Cocktail)
        {
            var add = amount;
            if (_rmcReagent.TryIndex(reagent, out var proto) && proto.Overdose is { } overdose)
            {
                var current = solution.GetTotalPrototypeQuantity(reagent);
                if (current + add > overdose)
                {
                    if (plate.OverdoseProtection == RMCEmergencyInjectorOverdose.Dynamic)
                    {
                        add = FixedPoint2.Max(FixedPoint2.Zero, overdose - current);
                        adjusted = true;
                    }
                    else
                    {
                        overdosed = true;
                    }
                }
            }

            if (add > FixedPoint2.Zero)
                toInject.AddReagent(reagent, add);
        }

        plate.Used = true;
        Dirty(plateUid, plate);

        if (toInject.Volume > FixedPoint2.Zero)
        {
            _reactive.DoEntityReaction(wearer, toInject, ReactionMethod.Injection);
            _solution.TryAddSolution(solutionEnt.Value, toInject);
        }

        if (overdosed)
            _popup.PopupEntity(Loc.GetString("rmc-armor-plate-injector-overdose"), wearer, wearer, PopupType.MediumCaution);
        else if (adjusted)
            _popup.PopupEntity(Loc.GetString("rmc-armor-plate-injector-adjusted"), wearer, wearer, PopupType.MediumCaution);

        _audio.PlayPvs(plate.InjectSound, wearer);
        args.Handled = true;
    }

    private void OnEmergencyInjectorToggleOverdose(Entity<RMCEmergencyInjectorPlateActiveComponent> ent, ref RMCEmergencyInjectorToggleOverdoseActionEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Plate is not { } plateUid || !TryComp(plateUid, out RMCEmergencyInjectorPlateComponent? plate))
            return;

        plate.OverdoseProtection = plate.OverdoseProtection switch
        {
            RMCEmergencyInjectorOverdose.Dynamic => RMCEmergencyInjectorOverdose.Off,
            RMCEmergencyInjectorOverdose.Off => RMCEmergencyInjectorOverdose.Strict,
            _ => RMCEmergencyInjectorOverdose.Dynamic,
        };
        Dirty(plateUid, plate);

        var text = plate.OverdoseProtection switch
        {
            RMCEmergencyInjectorOverdose.Dynamic => "rmc-armor-plate-injector-mode-dynamic",
            RMCEmergencyInjectorOverdose.Strict => "rmc-armor-plate-injector-mode-strict",
            _ => "rmc-armor-plate-injector-mode-off",
        };
        _popup.PopupEntity(Loc.GetString(text), ent.Owner, ent.Owner);

        args.Handled = true;
    }

    private void OnCeramicDamageModify(Entity<RMCCeramicPlateActiveComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Tool is not { } tool || !HasComp<ProjectileComponent>(tool) || HasComp<RMCArmorPlateBypassComponent>(tool))
            return;

        if (ent.Comp.Plate is not { } plateUid || !TryComp(plateUid, out RMCCeramicPlateComponent? plate) || plate.Broken)
            return;

        var incoming = args.Damage.GetTotal();
        if (incoming <= FixedPoint2.Zero)
            return;

        args.Damage = new DamageSpecifier();

        if (_net.IsClient)
            return;

        var mult = plate.HostileDurabilityMult;
        if (args.Origin is { } origin && _faction.IsEntityFriendly(ent.Owner, origin))
            mult = plate.FriendlyFireDurabilityMult;

        plate.Health -= incoming.Float() * mult;

        if (plate.Health <= 0)
        {
            plate.Health = 0;
            plate.Broken = true;
            _popup.PopupEntity(Loc.GetString("rmc-armor-plate-break", ("plate", plateUid)), ent.Owner, ent.Owner, PopupType.MediumCaution);
            _audio.PlayPvs(plate.BreakSound, ent.Owner);
        }

        _audio.PlayPvs(plate.HitSound, ent.Owner);

        UpdateCeramicVisuals(plateUid, plate);
        Dirty(plateUid, plate);
    }

    private void UpdateCeramicVisuals(EntityUid plateUid, RMCCeramicPlateComponent plate)
    {
        var state = RMCCeramicPlateVisualState.Full;
        if (plate.Broken || plate.Health <= 0)
            state = RMCCeramicPlateVisualState.Broken;
        else if (plate.MaxHealth > 0 && plate.Health <= plate.MaxHealth / 2f)
            state = RMCCeramicPlateVisualState.Damaged;

        _appearance.SetData(plateUid, RMCCeramicPlateVisuals.State, state);
    }
}
