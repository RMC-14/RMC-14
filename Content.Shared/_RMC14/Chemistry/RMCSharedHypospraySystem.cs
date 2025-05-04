﻿using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Chemistry;

public abstract class RMCSharedHypospraySystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] protected readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] protected readonly SkillsSystem _skills = default!;
    [Dependency] protected readonly ItemSlotsSystem _slots = default!;
    [Dependency] protected readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] protected readonly UseDelaySystem _useDelay = default!;
    [Dependency] protected readonly SolutionTransferSystem _transfer = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCHyposprayComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferVerbs);
        SubscribeLocalEvent<RMCHyposprayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCHyposprayComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<RMCHyposprayComponent, EntRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<RMCHyposprayComponent, AfterInteractEvent>(OnInteractAfter);
        SubscribeLocalEvent<RMCHyposprayComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCHyposprayComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RMCHyposprayComponent, InteractUsingEvent>(OnInteractUsing);

    }

    private void OnExamine(Entity<RMCHyposprayComponent> ent, ref ExaminedEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container) || container.ContainedEntities.Count == 0)
            return;

        var vial = container.ContainedEntities[0];

        args.PushText(Loc.GetString("rmc-hypospray-loaded", ("vial", vial)));
    }

    private void AddSetTransferVerbs(Entity<RMCHyposprayComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var (_, component) = entity;

        var priority = 0;

        // Add specific transfer verbs according to the container's size
        foreach (var amount in entity.Comp.TransferAmounts)
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    component.TransferAmount = amount;
                    _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), user, user);
                    Dirty(entity);
                },

                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority
            };

            priority -= 1;

            args.Verbs.Add(verb);
        }
    }

    private void OnStartup(Entity<RMCHyposprayComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnInsert(Entity<RMCHyposprayComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!ent.Comp.Initialized)
            return;

        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateAppearance(ent);
    }

    private void OnRemove(Entity<RMCHyposprayComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateAppearance(ent);
    }

    private void OnUseInHand(Entity<RMCHyposprayComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        int index = Array.IndexOf(ent.Comp.TransferAmounts, ent.Comp.TransferAmount) + 1;

        if (index >= ent.Comp.TransferAmounts.Length)
            index = 0;
        ent.Comp.TransferAmount = ent.Comp.TransferAmounts[index];
        _popup.PopupClient(Loc.GetString("rmc-hypospray-amount-change", ("amount", ent.Comp.TransferAmount)), args.User, args.User);
        Dirty(ent);

        args.Handled = true;
    }

    private void OnInteractAfter(Entity<RMCHyposprayComponent> ent, ref AfterInteractEvent args)
    {

        if (args.Handled || !args.CanReach)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;

        if (args.Target == null)
            return;

        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (_slots.CanInsert(ent, args.Target.Value, args.User, slots.Slots[ent.Comp.SlotId], true))
        {
            args.Handled = true;
            if (!_skills.HasSkills(args.User, ent.Comp.TacticalSkills))
            {
                _popup.PopupClient(Loc.GetString("rmc-hypospray-fail-tacreload"), args.Used, args.User);
                return;
            }
            //Tactical reload
            if (container.ContainedEntities.Count == 0)
            {
                _popup.PopupClient(Loc.GetString("rmc-hypospray-load-tacreload", ("hypo", ent)), args.Used, args.User);
            }
            else
            {
                if (!_slots.TryEjectToHands(ent, slots.Slots[ent.Comp.SlotId], args.User, true))
                    return;
                _popup.PopupClient(Loc.GetString("rmc-hypospray-swap-tacreload"), args.Used, args.User);
            }

            _doafter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.TacticalReloadTime, new TacticalReloadHyposprayDoAfterEvent(), ent, args.Target, ent)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
                NeedHand = ent.Comp.NeedHand,
                BreakOnHandChange = ent.Comp.BreakOnHandChange,
                MovementThreshold = ent.Comp.MovementThreshold
            });
            return;
        }

        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-hypospray-no-vial"), ent, args.User);
            args.Handled = true;
            return;
        }

        var vial = container.ContainedEntities[0];

        if (HasComp<InjectableSolutionComponent>(args.Target.Value) && (!ent.Comp.OnlyAffectsMobs || HasComp<MobStateComponent>(args.Target.Value)))
        {
            // Try to inject
            args.Handled = true;
            if (TryComp(ent, out UseDelayComponent? delayComp))
            {
                if (_useDelay.IsDelayed((ent, delayComp)))
                    return;
            }

            var attemptEv = new AttemptHyposprayUseEvent(args.User, args.Target.Value, TimeSpan.Zero);

            RaiseLocalEvent(ent, ref attemptEv);
            var doAfter = new HyposprayDoAfterEvent();
            var argsu = new DoAfterArgs(EntityManager, args.User, attemptEv.DoAfter, doAfter, ent, args.Target, ent)
            {
                BreakOnMove = true,
                BreakOnHandChange = true,
                NeedHand = true
            };
            _doafter.TryStartDoAfter(argsu);
        }
    }

    protected virtual void OnInteractUsing(Entity<RMCHyposprayComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;


        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;
        // Dont transfer when vial is used
        if (_slots.CanInsert(ent, args.Used, args.User, slots.Slots[ent.Comp.SlotId], true))
            return;

        var vial = container.ContainedEntities[0];

        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hypospray-no-vial"), ent, args.User);
            return;
        }

        if (!_solution.TryGetRefillableSolution(vial, out var solm, out var soli))
            return;

        if (!_solution.TryGetDrainableSolution(args.Used, out var soln, out var solu))
            return;

        if (!TryComp<SolutionTransferComponent>(args.Used, out var solt))
            return;

        args.Handled = true;

        var transferr = _transfer.Transfer(args.User, args.Used, soln.Value, vial, solm.Value, solt.TransferAmount);

        if (transferr > 0)
        {
            var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferr), ("target", vial));
            _popup.PopupClient(message, ent, args.User);
        }

        Dirty(soln.Value);
        Dirty(solm.Value);

        UpdateAppearance(ent);
    }


    protected void UpdateAppearance(Entity<RMCHyposprayComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;

        int containerEnts = container.ContainedEntities.Count;

        _appearance.SetData(ent, VialVisuals.Occupied, containerEnts != 0, appearance);

        if (!HasComp<SolutionContainerVisualsComponent>(ent))
            return;

        Solution? solution;
        if (containerEnts == 0)
            solution = new Solution();
        else
        {
            var vial = container.ContainedEntities[0];

            if (!_solution.TryGetSolution(vial, ent.Comp.VialName, out var soln))
                return;

            solution = soln.Value.Comp.Solution;
        }

        _appearance.SetData(ent, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearance);
        _appearance.SetData(ent, SolutionContainerVisuals.Color, solution.GetColor(_prototype), appearance);
        _appearance.SetData(ent, SolutionContainerVisuals.SolutionName, ent.Comp.SolutionName, appearance);

        if (solution.GetPrimaryReagentId() is { } reagent)
            _appearance.SetData(ent, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearance);

        Dirty(ent, ent.Comp);
    }
}
