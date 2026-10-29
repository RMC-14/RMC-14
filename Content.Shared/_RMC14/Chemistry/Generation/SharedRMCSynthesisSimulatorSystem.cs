using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCSynthesisSimulatorSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected readonly INetManager Net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] private readonly SharedRMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSynthesisSimulatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCSynthesisSimulatorComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<RMCSynthesisSimulatorComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<RMCSynthesisSimulatorComponent, EntRemovedFromContainerMessage>(OnSlotChanged);

        Subs.BuiEvents<RMCSynthesisSimulatorComponent>(RMCSynthesisSimulatorUi.Key,
            subs =>
            {
                subs.Event<RMCSynthesisSimulatorSetModeBuiMsg>(OnSetModeMsg);
                subs.Event<RMCSynthesisSimulatorSelectTargetPropertyBuiMsg>(OnSelectTargetPropertyMsg);
                subs.Event<RMCSynthesisSimulatorSelectReferencePropertyBuiMsg>(OnSelectReferencePropertyMsg);
                subs.Event<RMCSynthesisSimulatorSimulateBuiMsg>(OnSimulateMsg);
                subs.Event<RMCSynthesisSimulatorPickRecipeBuiMsg>(OnPickRecipeMsg);
                subs.Event<RMCSynthesisSimulatorEjectTargetBuiMsg>(OnEjectTargetMsg);
                subs.Event<RMCSynthesisSimulatorEjectReferenceBuiMsg>(OnEjectReferenceMsg);
                subs.Event<RMCSynthesisSimulatorCancelSimulationBuiMsg>(OnCancelSimulationMsg);
            });
    }

    private void OnMapInit(Entity<RMCSynthesisSimulatorComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.TargetSlotId);
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ReferenceSlotId);
        UpdateAppearance(ent);
    }

    protected void UpdateAppearance(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        var state = ent.Comp.PrintingUntil != null ? RMCSynthesisSimulatorVisualState.Printing :
            ent.Comp.ReadingUntil != null ? RMCSynthesisSimulatorVisualState.Reading :
            ent.Comp.Picking ? RMCSynthesisSimulatorVisualState.Ready :
            ent.Comp.Simulating ? RMCSynthesisSimulatorVisualState.Running :
            RMCSynthesisSimulatorVisualState.Idle;

        Appearance.SetData(ent, RMCSynthesisSimulatorVisuals.State, state);
    }

    protected void StartPrintingAnimation(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        ent.Comp.PrintingUntil = Timing.CurTime + ent.Comp.PrintingDuration;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnInsertAttempt(Entity<RMCSynthesisSimulatorComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Simulating || ent.Comp.Picking || !HasComp<RMCChemResearchReportComponent>(args.Item))
        {
            args.Cancelled = true;
            return;
        }

        if (args.Slot.ID == ent.Comp.ReferenceSlotId && ent.Comp.Mode is not (SynthesisMode.Relate or SynthesisMode.Add))
            args.Cancelled = true;
    }

    private void OnSlotChanged(Entity<RMCSynthesisSimulatorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.TargetSlotId && args.Container.ID != ent.Comp.ReferenceSlotId)
            return;

        ent.Comp.SimulationFailed = false;
        if (Net.IsServer)
        {
            ent.Comp.ReadingUntil = Timing.CurTime + ent.Comp.ReadingDuration;
            UpdateAppearance(ent);
        }

        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnSlotChanged(Entity<RMCSynthesisSimulatorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.TargetSlotId && args.Container.ID != ent.Comp.ReferenceSlotId)
            return;

        ent.Comp.SimulationFailed = false;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnSetModeMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorSetModeBuiMsg args)
    {
        if (ent.Comp.Simulating || ent.Comp.Picking)
            return;

        ent.Comp.Mode = args.Mode;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnSelectTargetPropertyMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorSelectTargetPropertyBuiMsg args)
    {
        if (ent.Comp.Simulating || ent.Comp.Picking)
            return;

        ent.Comp.TargetProperty = args.PropertyId;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnSelectReferencePropertyMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorSelectReferencePropertyBuiMsg args)
    {
        if (ent.Comp.Simulating || ent.Comp.Picking)
            return;

        ent.Comp.ReferenceProperty = args.PropertyId;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnEjectTargetMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorEjectTargetBuiMsg args)
    {
        if (ent.Comp.Simulating || ent.Comp.Picking || !ItemSlots.TryGetSlot(ent, ent.Comp.TargetSlotId, out var slot))
            return;

        if (!ItemSlots.TryEjectToHands(ent, slot, args.Actor, true))
            return;

        ent.Comp.TargetProperty = null;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnEjectReferenceMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorEjectReferenceBuiMsg args)
    {
        if (ent.Comp.Simulating || ent.Comp.Picking || !ItemSlots.TryGetSlot(ent, ent.Comp.ReferenceSlotId, out var slot))
            return;

        if (!ItemSlots.TryEjectToHands(ent, slot, args.Actor, true))
            return;

        ent.Comp.ReferenceProperty = null;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnCancelSimulationMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorCancelSimulationBuiMsg args)
    {
        if (!ent.Comp.Simulating && !ent.Comp.Picking)
            return;

        ent.Comp.Simulating = false;
        ent.Comp.Picking = false;
        ent.Comp.RecipeCandidates = null;
        Dirty(ent);
        UpdateAppearance(ent);
        RefreshUIs(ent);
    }

    private void OnSimulateMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorSimulateBuiMsg args)
    {
        if (ent.Comp.Simulating || ent.Comp.Picking)
            return;

        if (!CheckReady(ent, out var reason))
        {
            _popup.PopupClient(reason, ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Simulating = true;
        ent.Comp.FinishAt = Timing.CurTime + ent.Comp.ProcessDuration;
        Dirty(ent);
        UpdateAppearance(ent);
        RefreshUIs(ent);
        _audio.PlayPredicted(ent.Comp.StartSound, ent, args.Actor);
    }

    private void OnPickRecipeMsg(Entity<RMCSynthesisSimulatorComponent> ent, ref RMCSynthesisSimulatorPickRecipeBuiMsg args)
    {
        if (!ent.Comp.Picking ||
            ent.Comp.RecipeCandidates is not { } candidates ||
            args.Index < 0 ||
            args.Index >= candidates.Count)
        {
            return;
        }

        FinalizeSimulation(ent, args.Index);
        RefreshUIs(ent);
    }

    protected virtual void DoSimulation(Entity<RMCSynthesisSimulatorComponent> ent)
    {
    }

    protected virtual void FinalizeSimulation(Entity<RMCSynthesisSimulatorComponent> ent, int recipeIndex)
    {
    }

    protected virtual void RefreshUIs(Entity<RMCSynthesisSimulatorComponent> ent)
    {
    }

    public override void Update(float frameTime)
    {
        if (Net.IsClient)
            return;

        var time = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSynthesisSimulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var animationEnded = false;
            if (comp.ReadingUntil is { } readingUntil && time >= readingUntil)
            {
                comp.ReadingUntil = null;
                animationEnded = true;
            }

            if (comp.PrintingUntil is { } printingUntil && time >= printingUntil)
            {
                comp.PrintingUntil = null;
                animationEnded = true;
            }

            if (animationEnded)
            {
                Dirty(uid, comp);
                UpdateAppearance((uid, comp));
            }

            if (!comp.Simulating || time < comp.FinishAt)
                continue;

            DoSimulation((uid, comp));
        }
    }

    public bool TryGetSlotReport(Entity<RMCSynthesisSimulatorComponent> ent, string slotId, out EntityUid item, out RMCChemResearchReportComponent report)
    {
        item = default;
        report = default!;
        if (!ItemSlots.TryGetSlot(ent, slotId, out var slot) || slot.ContainerSlot?.ContainedEntity is not { } contained)
            return false;

        if (!TryComp(contained, out RMCChemResearchReportComponent? reportComp))
            return false;

        item = contained;
        report = reportComp;
        return true;
    }

    public List<(ChemGeneratorPropertyPrototype Property, int Level)> GetReportProperties(RMCChemResearchReportComponent report)
    {
        var result = new List<(ChemGeneratorPropertyPrototype, int)>();
        foreach (var p in report.Properties)
        {
            if (ProtoManager.TryIndex<ChemGeneratorPropertyPrototype>(p.PropertyId, out var proto))
                result.Add((proto, p.Level));
        }

        return result;
    }

    public bool TryGetProperty(RMCChemResearchReportComponent report, string? propertyId, out ChemGeneratorPropertyPrototype property, out int level)
    {
        property = default!;
        level = 0;
        if (propertyId == null)
            return false;

        foreach (var p in report.Properties)
        {
            if (p.PropertyId != propertyId)
                continue;

            if (!ProtoManager.TryIndex<ChemGeneratorPropertyPrototype>(p.PropertyId, out var proto))
                return false;

            property = proto;
            level = p.Level;
            return true;
        }

        return false;
    }

    private bool HasUnregisteredComponents(string reagentId, RMCChemistryResearchComponent research)
    {
        var recipe = ProtoManager.EnumeratePrototypes<ReactionPrototype>().FirstOrDefault(r => r.Products.ContainsKey(reagentId));
        if (recipe == null)
            return false;

        foreach (var componentId in recipe.Reactants.Keys)
        {
            if (!_rmcReagent.TryIndex(componentId, out var component))
                continue;

            if (component.ChemClass >= ChemClass.Special &&
                component.ChemClass != ChemClass.Hydro &&
                !research.IdentifiedIds.Contains(componentId))
            {
                return true;
            }
        }

        return false;
    }

    public int GetCost(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        var comp = ent.Comp;
        if (!TryGetSlotReport(ent, comp.TargetSlotId, out _, out var targetReport))
            return 0;

        var targetProperties = GetReportProperties(targetReport);
        var targetTotalValue = targetProperties.Sum(p => p.Property.Value);
        var allPositive = targetProperties.All(p => p.Property.Category == ChemPropertyCategory.Positive);

        ChemGeneratorPropertyPrototype? referenceProperty = null;
        var referenceLevel = 0;
        if (TryGetSlotReport(ent, comp.ReferenceSlotId, out _, out var referenceReport))
            TryGetProperty(referenceReport, comp.ReferenceProperty, out referenceProperty!, out referenceLevel);

        int cost;
        if (comp.Mode == SynthesisMode.Add)
        {
            if (referenceProperty == null)
                return 0;

            cost = Math.Clamp(referenceLevel + referenceProperty.Value - 1, 1, comp.PropertyCostMax);
            if (targetProperties.Count > comp.AddBulkThreshold)
                cost += comp.AddBulkMultiplier;
            if (targetTotalValue > comp.AddValueThreshold)
                cost += comp.AddValueMultiplier;
        }
        else
        {
            if (!TryGetProperty(targetReport, comp.TargetProperty, out var targetProperty, out var targetLevel))
                return 0;

            if (targetProperty.Flags.HasFlag(ChemPropertyType.Anomalous))
            {
                cost = targetLevel * comp.AnomalousMultiplier;
            }
            else
            {
                cost = comp.Mode switch
                {
                    SynthesisMode.Amplify => Math.Clamp(targetLevel + targetProperty.Value - 1, 1, comp.PropertyCostMax),
                    SynthesisMode.Suppress => 2,
                    SynthesisMode.Relate when referenceProperty != null =>
                        referenceProperty.Flags.HasFlag(ChemPropertyType.Anomalous) ? targetLevel * comp.RelateAnomalousMultiplier :
                        referenceProperty.Rarity < ChemPropertyRarity.Rare ? targetLevel :
                        targetLevel * comp.RareMultiplier + targetProperty.Value,
                    SynthesisMode.Relate => targetLevel + targetProperty.Value,
                    _ => 0,
                };
            }
        }

        if (!allPositive)
            cost = Math.Max(cost - comp.NonPositiveDiscount, 1);

        return cost;
    }

    public int? GetPredictedOverdose(Entity<RMCSynthesisSimulatorComponent> ent)
    {
        if (!TryGetSlotReport(ent, ent.Comp.TargetSlotId, out _, out var targetReport))
            return null;

        var overdose = Math.Max(targetReport.Overdose, 1);
        if (ent.Comp.Mode != SynthesisMode.Add)
            overdose = overdose <= 5 ? Math.Max(overdose - 1, 1) : Math.Max(overdose - 5, 5);

        return overdose;
    }

    public bool CheckReady(Entity<RMCSynthesisSimulatorComponent> ent, out string reason)
    {
        var comp = ent.Comp;

        if (comp.SimulationFailed)
        {
            reason = "CRITICAL FAILURE: NO POSSIBLE CHEMICAL COMBINATION";
            return false;
        }

        if (!TryGetSlotReport(ent, comp.TargetSlotId, out _, out var targetReport))
        {
            reason = "No target research report inserted.";
            return false;
        }

        if (!targetReport.Completed)
        {
            reason = "Incomplete data detected in target report.";
            return false;
        }

        if (!targetReport.IsGenerated || targetReport.ReagentId is not { } targetId)
        {
            reason = "Target chemical cannot be altered.";
            return false;
        }

        var research = _research.TryGetResearch(out var researchEnt)
            ? researchEnt.Value.Comp
            : new RMCChemistryResearchComponent();

        if (research.LockedIds.Contains(targetId.Id))
        {
            reason = "Target chemical structure destroyed.";
            return false;
        }

        if (HasUnregisteredComponents(targetId.Id, research))
        {
            reason = "Unregistered components detected.";
            return false;
        }

        var targetProperties = GetReportProperties(targetReport);
        if (targetProperties.Count < 2)
        {
            reason = "Target complexity improper for simulation.";
            return false;
        }

        var credits = research.Credits;
        var clearance = research.ClearanceLevel;

        if (comp.Mode != SynthesisMode.Add)
        {
            if (!TryGetProperty(targetReport, comp.TargetProperty, out var targetProperty, out var targetLevel))
            {
                reason = "No target property selected.";
                return false;
            }

            if (targetProperty.Flags.HasFlag(ChemPropertyType.Unadjustable))
            {
                reason = "That property cannot be simulated.";
                return false;
            }

            if (GetCost(ent) > credits)
            {
                reason = "Insufficient research credits.";
                return false;
            }

            if (comp.Mode == SynthesisMode.Amplify)
            {
                if (clearance < comp.MaxClearanceLevel && targetLevel >= clearance * comp.TechtreeLevelMultiplier + 2)
                {
                    reason = "Clearance insufficient for amplification.";
                    return false;
                }

                if (targetLevel >= targetProperty.MaxLevel)
                {
                    reason = "Property cannot be amplified further.";
                    return false;
                }
            }
        }

        if (comp.Mode is SynthesisMode.Relate or SynthesisMode.Add)
        {
            if (!TryGetSlotReport(ent, comp.ReferenceSlotId, out _, out var referenceReport))
            {
                reason = "No reference research report inserted.";
                return false;
            }

            if (!referenceReport.Completed)
            {
                reason = "Incomplete data detected in reference report.";
                return false;
            }

            if (referenceReport.ReagentId is { } referenceId && research.LockedIds.Contains(referenceId.Id))
            {
                reason = "Reference chemical structure destroyed.";
                return false;
            }

            if (!TryGetProperty(referenceReport, comp.ReferenceProperty, out var referenceProperty, out var referenceLevel))
            {
                reason = "No reference property selected.";
                return false;
            }

            if (referenceProperty.Flags.HasFlag(ChemPropertyType.Unadjustable))
            {
                reason = "That property cannot be simulated.";
                return false;
            }

            if (targetProperties.Any(p => p.Property.ID == referenceProperty.ID))
            {
                reason = "Reference property already present in target.";
                return false;
            }

            if (comp.Mode == SynthesisMode.Relate &&
                TryGetProperty(targetReport, comp.TargetProperty, out _, out var targetLevelForRelate) &&
                targetLevelForRelate != referenceLevel)
            {
                reason = "Reference and target property must be of equal levels.";
                return false;
            }

            if (comp.Mode == SynthesisMode.Add && GetCost(ent) > credits)
            {
                reason = "Insufficient research credits.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }
}
