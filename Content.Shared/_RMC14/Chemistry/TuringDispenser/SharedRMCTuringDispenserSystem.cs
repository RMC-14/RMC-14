using Content.Shared._RMC14.Chemistry.Centrifuge;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.TuringDispenser;

public sealed class SharedRMCTuringDispenserSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedRMCCentrifugeSystem _centrifuge = default!;
    [Dependency] private readonly CMRefillableSolutionSystem _cmRefillable = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _cmVendor = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedRMCSmartFridgeSystem _smartFridge = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId VialPrototype = "RMCVial";

    private static readonly FixedPoint2 MinMultiplier = FixedPoint2.New(0.1);
    private static readonly FixedPoint2 MaxMultiplier = FixedPoint2.New(10);

    private const int MinCycles = 1;
    private const int MaxCycles = 100;

    public static readonly (EntProtoId Id, FixedPoint2 MaxVol, bool VendorStocked)[] BeakerOptions =
    [
        ("RMCVial", 30, false),
        ("CMBeaker", 60, true),
        ("CMBeakerLarge", 120, true),
    ];

    private readonly HashSet<Entity<RMCSmartFridgeComponent>> _smartFridges = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCTuringDispenserComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCTuringDispenserComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<RMCTuringDispenserComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<RMCTuringDispenserComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);

        Subs.BuiEvents<RMCTuringDispenserComponent>(RMCTuringDispenserUi.Key,
            subs =>
            {
                subs.Event<RMCTuringDispenserRunProgramBuiMsg>(OnRunProgramMsg);
                subs.Event<RMCTuringDispenserSaveToMemoryBuiMsg>(OnSaveToMemoryMsg);
                subs.Event<RMCTuringDispenserClearMemoryBuiMsg>(OnClearMemoryMsg);
                subs.Event<RMCTuringDispenserEjectBoxBuiMsg>(OnEjectBoxMsg);
                subs.Event<RMCTuringDispenserEjectBeakerBuiMsg>(OnEjectBeakerMsg);
                subs.Event<RMCTuringDispenserDisposeBeakerBuiMsg>(OnDisposeBeakerMsg);
                subs.Event<RMCTuringDispenserSetMultiplierBuiMsg>(OnSetMultiplierMsg);
                subs.Event<RMCTuringDispenserSetCyclesBuiMsg>(OnSetCyclesMsg);
                subs.Event<RMCTuringDispenserToggleAutoRunBuiMsg>(OnToggleAutoRunMsg);
                subs.Event<RMCTuringDispenserToggleSmartLinkBuiMsg>(OnToggleSmartLinkMsg);
                subs.Event<RMCTuringDispenserToggleOutputModeBuiMsg>(OnToggleOutputModeMsg);
                subs.Event<RMCTuringDispenserSetPreferredBeakerBuiMsg>(OnSetPreferredBeakerMsg);
            });
    }

    private void OnMapInit(Entity<RMCTuringDispenserComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.InputBoxSlotId);
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.OutputBeakerSlotId);
        ent.Comp.NextRecharge = _timing.CurTime + ent.Comp.RechargeEvery;
        ent.Comp.NextProcess = _timing.CurTime;
    }

    private void OnInsertAttempt(Entity<RMCTuringDispenserComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Slot.ContainerSlot?.ID == ent.Comp.InputBoxSlotId && ent.Comp.Status == TuringDispenserStatus.Running)
            args.Cancelled = true;
    }

    private void OnEntInserted(Entity<RMCTuringDispenserComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.InputBoxSlotId)
        {
            _appearance.SetData(ent, TuringDispenserVisuals.HasBox, true);
            BuildProgram(ent, TuringDispenserProgram.Box, args.Entity);
        }
        else if (args.Container.ID == ent.Comp.OutputBeakerSlotId)
        {
            _appearance.SetData(ent, TuringDispenserVisuals.HasBeaker, true);
        }
        else
        {
            return;
        }

        if (ent.Comp is { AutoRun: true, OutputMode: TuringDispenserOutputMode.Container } &&
            ent.Comp.Status is TuringDispenserStatus.Idle or TuringDispenserStatus.Finished &&
            _itemSlots.TryGetSlot(ent, ent.Comp.InputBoxSlotId, out var boxSlot) && boxSlot.ContainerSlot?.ContainedEntity != null &&
            _itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var beakerSlot) && beakerSlot.ContainerSlot?.ContainedEntity != null)
        {
            StartProgram(ent);
        }
    }

    private void OnEntRemoved(Entity<RMCTuringDispenserComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.OutputBeakerSlotId)
        {
            _appearance.SetData(ent, TuringDispenserVisuals.HasBeaker, false);
            StopProgram(ent);
            return;
        }

        if (args.Container.ID != ent.Comp.InputBoxSlotId)
            return;

        _appearance.SetData(ent, TuringDispenserVisuals.HasBox, false);
        ent.Comp.BoxProgram.Clear();
        Dirty(ent);
        StopProgram(ent);
    }

    private void BuildProgram(Entity<RMCTuringDispenserComponent> ent, TuringDispenserProgram program, EntityUid box)
    {
        if (!TryComp(box, out StorageComponent? storage))
            return;

        var entries = new List<TuringProgramEntry>();
        foreach (var vial in storage.Container.ContainedEntities)
        {
            if (!_solution.TryGetSolution(vial, "beaker", out _, out var solution) ||
                solution.Contents.Count != 1)
            {
                continue;
            }

            var content = solution.Contents[0];
            ProtoId<ReagentPrototype> reagent = content.Reagent.Prototype;

            var index = entries.FindIndex(e => e.Reagent == reagent);
            if (index >= 0)
                entries[index] = entries[index] with { Amount = entries[index].Amount + content.Quantity };
            else
                entries.Add(new TuringProgramEntry(reagent, content.Quantity));
        }

        if (program == TuringDispenserProgram.Memory)
            ent.Comp.MemoryProgram = entries;
        else
            ent.Comp.BoxProgram = entries;

        Dirty(ent);
    }

    private void OnRunProgramMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserRunProgramBuiMsg args)
    {
        if (ent.Comp.OutputMode == TuringDispenserOutputMode.Centrifuge)
            return;

        StartProgram(ent);
    }

    public void StartProgram(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        comp.ActiveProgram = comp.MemoryProgram.Count > 0 ? TuringDispenserProgram.Memory : TuringDispenserProgram.Box;
        var program = comp.ActiveProgram == TuringDispenserProgram.Memory ? comp.MemoryProgram : comp.BoxProgram;

        if (program.Count == 0)
        {
            SetStatus(ent, TuringDispenserStatus.Idle);
            return;
        }

        if (comp.OutputMode == TuringDispenserOutputMode.Container &&
            (!_itemSlots.TryGetSlot(ent, comp.OutputBeakerSlotId, out var slot) ||
             slot.ContainerSlot?.ContainedEntity is null))
        {
            SetStatus(ent, TuringDispenserStatus.Idle);
            return;
        }

        if (comp.Status == TuringDispenserStatus.Running)
            return;

        comp.Stage = 0;
        comp.Cycle = 0;
        comp.StageMissing = FixedPoint2.Zero;
        comp.Error = null;
        comp.FlushedContainers.Clear();
        SetStatus(ent, TuringDispenserStatus.Running);
    }

    private void OnSaveToMemoryMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSaveToMemoryBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.InputBoxSlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } box)
        {
            return;
        }

        BuildProgram(ent, TuringDispenserProgram.Memory, box);
    }

    private void OnClearMemoryMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserClearMemoryBuiMsg args)
    {
        ent.Comp.MemoryProgram.Clear();
        ent.Comp.ActiveProgram = TuringDispenserProgram.Box;
        Dirty(ent);
        StopProgram(ent);
    }

    private void StopProgram(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        comp.Stage = 0;
        comp.Cycle = 0;
        comp.StageMissing = FixedPoint2.Zero;
        comp.Error = null;

        if (comp.OutputMode == TuringDispenserOutputMode.SmartFridge)
            FlushBuffer(ent);

        SetStatus(ent, TuringDispenserStatus.Idle);
    }

    private void OnEjectBoxMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserEjectBoxBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.InputBoxSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);
    }

    private void OnEjectBeakerMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserEjectBeakerBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);
    }

    private void OnDisposeBeakerMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserDisposeBeakerBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } beaker ||
            !_solution.TryGetMixableSolution(beaker, out var solnEnt, out _))
        {
            return;
        }

        _solution.RemoveAllSolution(solnEnt.Value);
    }

    private void OnSetMultiplierMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSetMultiplierBuiMsg args)
    {
        if (args.Multiplier < MinMultiplier || args.Multiplier > MaxMultiplier)
            return;

        ent.Comp.Multiplier = args.Multiplier;
        Dirty(ent);
    }

    private void OnSetCyclesMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSetCyclesBuiMsg args)
    {
        if (args.Cycles < MinCycles || args.Cycles > MaxCycles)
            return;

        ent.Comp.CycleLimit = args.Cycles;
        Dirty(ent);
    }

    private void OnToggleAutoRunMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserToggleAutoRunBuiMsg args)
    {
        ent.Comp.AutoRun = !ent.Comp.AutoRun;
        Dirty(ent);
    }

    private void OnToggleSmartLinkMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserToggleSmartLinkBuiMsg args)
    {
        ent.Comp.SmartLink = !ent.Comp.SmartLink;
        Dirty(ent);
    }

    private void OnToggleOutputModeMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserToggleOutputModeBuiMsg args)
    {
        var comp = ent.Comp;

        if (comp.OutputMode != TuringDispenserOutputMode.Container)
            FlushBuffer(ent);

        comp.OutputMode = comp.OutputMode switch
        {
            TuringDispenserOutputMode.Container => HasNearbySmartFridge(ent)
                ? TuringDispenserOutputMode.SmartFridge
                : HasNearbyCentrifuge(ent)
                    ? TuringDispenserOutputMode.Centrifuge
                    : TuringDispenserOutputMode.Container,
            TuringDispenserOutputMode.SmartFridge => HasNearbyCentrifuge(ent)
                ? TuringDispenserOutputMode.Centrifuge
                : TuringDispenserOutputMode.Container,
            _ => TuringDispenserOutputMode.Container,
        };

        Dirty(ent);
    }

    private void OnSetPreferredBeakerMsg(Entity<RMCTuringDispenserComponent> ent, ref RMCTuringDispenserSetPreferredBeakerBuiMsg args)
    {
        if (args.Beaker is not { } beaker)
        {
            ent.Comp.PreferredBeaker = null;
            Dirty(ent);
            return;
        }

        foreach (var option in BeakerOptions)
        {
            if (option.Id != beaker)
                continue;

            ent.Comp.PreferredBeaker = beaker;
            Dirty(ent);
            return;
        }
    }

    private bool HasNearbySmartFridge(Entity<RMCTuringDispenserComponent> ent)
    {
        _smartFridges.Clear();
        _entityLookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.SmartFridgeRange, _smartFridges);
        return _smartFridges.Count > 0;
    }

    private bool HasNearbyCentrifuge(Entity<RMCTuringDispenserComponent> ent)
    {
        return _centrifuge.HasNearbyCentrifuge(Transform(ent).Coordinates, ent.Comp.CentrifugeRange);
    }

    private void FlushBuffer(Entity<RMCTuringDispenserComponent> ent)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolution, out var bufferEnt, out var buffer) ||
            buffer.Volume <= FixedPoint2.Zero)
        {
            return;
        }

        var coords = Transform(ent).Coordinates;
        var hasFridge = HasNearbySmartFridge(ent);

        if (!hasFridge)
        {
            _solution.RemoveAllSolution(bufferEnt.Value);
            return;
        }

        while (buffer.Volume > FixedPoint2.Zero)
        {
            var remaining = buffer.Volume;

            if (ent.Comp.PreferredBeaker is { } preferred)
                FlushIntoPreferredBeaker(ent, bufferEnt.Value, coords, remaining, preferred);
            else
                FlushIntoAutoBeaker(ent, bufferEnt.Value, coords, remaining);
        }
    }

    private void FlushIntoPreferredBeaker(Entity<RMCTuringDispenserComponent> ent, Entity<SolutionComponent> bufferEnt, EntityCoordinates coords, FixedPoint2 remaining, EntProtoId preferred)
    {
        if (_smartFridge.TryGetEmptyContainerByPrototype(coords, ent.Comp.SmartFridgeRange, preferred, out var existing) &&
            _solution.TryGetSolution(existing, "beaker", out var existingSolnEnt, out var existingSolution))
        {
            BottleInto(ent, bufferEnt, existing, existingSolnEnt.Value, existingSolution, remaining);
            return;
        }

        var isVendorStocked = false;
        foreach (var option in BeakerOptions)
        {
            if (option.Id == preferred)
                isVendorStocked = option.VendorStocked;
        }

        if (isVendorStocked &&
            _cmVendor.TryTakeStockedItem(coords, ent.Comp.SmartFridgeRange, preferred, out var vendorBeaker) &&
            _solution.TryGetSolution(vendorBeaker, "beaker", out var vendorSolnEnt, out var vendorSolution))
        {
            BottleInto(ent, bufferEnt, vendorBeaker, vendorSolnEnt.Value, vendorSolution, remaining);
            _smartFridge.TransferToNearby(coords, ent.Comp.SmartFridgeRange, vendorBeaker);
            return;
        }

        FlushIntoFreshVial(ent, bufferEnt, coords, remaining);
    }

    private void FlushIntoAutoBeaker(Entity<RMCTuringDispenserComponent> ent, Entity<SolutionComponent> bufferEnt, EntityCoordinates coords, FixedPoint2 remaining)
    {
        if (_smartFridge.TryGetEmptyContainer(coords, ent.Comp.SmartFridgeRange, remaining, out var existing) &&
            _solution.TryGetSolution(existing, "beaker", out var existingSolnEnt, out var existingSolution))
        {
            BottleInto(ent, bufferEnt, existing, existingSolnEnt.Value, existingSolution, remaining);
            return;
        }

        var best = BeakerOptions[^1];
        foreach (var option in BeakerOptions)
        {
            if (option.MaxVol >= remaining)
            {
                best = option;
                break;
            }
        }

        FlushIntoPreferredBeaker(ent, bufferEnt, coords, remaining, best.Id);
    }

    private void FlushIntoFreshVial(Entity<RMCTuringDispenserComponent> ent, Entity<SolutionComponent> bufferEnt, EntityCoordinates coords, FixedPoint2 remaining)
    {
        var vial = Spawn(VialPrototype, coords);
        if (!_solution.TryGetSolution(vial, "beaker", out var vialSolnEnt, out var vialSolution))
        {
            QueueDel(vial);
            _solution.SplitSolution(bufferEnt, FixedPoint2.Min(FixedPoint2.New(30), remaining));
            return;
        }

        BottleInto(ent, bufferEnt, vial, vialSolnEnt.Value, vialSolution, remaining);
        _smartFridge.TransferToNearby(coords, ent.Comp.SmartFridgeRange, vial);
    }

    private void BottleInto(Entity<RMCTuringDispenserComponent> ent, Entity<SolutionComponent> bufferEnt, EntityUid container, Entity<SolutionComponent> solnEnt, Solution solution, FixedPoint2 remaining)
    {
        var amount = FixedPoint2.Min(remaining, solution.AvailableVolume);
        var split = _solution.SplitSolution(bufferEnt, amount);
        _solution.TryAddSolution(solnEnt, split);
        ent.Comp.FlushedContainers.Add(container);

        var baseName = MetaData(container).EntityPrototype?.Name ?? "vial";
        if (split.Contents.Count == 1 && _rmcReagent.TryIndex(split.Contents[0].Reagent.Prototype, out var reagentProto))
            _metaData.SetEntityName(container, $"{baseName} ({reagentProto.LocalizedName})");
        else if (split.Contents.Count > 1)
            _metaData.SetEntityName(container, $"{baseName} (mixed)");
    }

    private bool TryGetTargetSolution(Entity<RMCTuringDispenserComponent> ent, out Entity<SolutionComponent>? solnEnt, out Solution? solution)
    {
        if (ent.Comp.OutputMode == TuringDispenserOutputMode.Container)
        {
            if (!_itemSlots.TryGetSlot(ent, ent.Comp.OutputBeakerSlotId, out var slot) ||
                slot.ContainerSlot?.ContainedEntity is not { } beaker ||
                !_solution.TryGetMixableSolution(beaker, out solnEnt, out solution))
            {
                solnEnt = null;
                solution = null;
                return false;
            }

            return true;
        }

        return _solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolution, out solnEnt, out solution);
    }

    private void AdvanceProgram(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        comp.Stage = 0;
        comp.StageMissing = FixedPoint2.Zero;

        if (comp.MemoryProgram.Count > 0 && comp.BoxProgram.Count > 0)
        {
            if (comp.ActiveProgram == TuringDispenserProgram.Box)
            {
                comp.Cycle++;
                comp.ActiveProgram = TuringDispenserProgram.Memory;
            }
            else
            {
                comp.ActiveProgram = TuringDispenserProgram.Box;
            }
        }
        else
        {
            comp.Cycle++;
        }

        if (comp.OutputMode == TuringDispenserOutputMode.SmartFridge)
            FlushBuffer(ent);

        Dirty(ent);
    }

    private void SetStatus(Entity<RMCTuringDispenserComponent> ent, TuringDispenserStatus status)
    {
        ent.Comp.Status = status;
        Dirty(ent);
        _appearance.SetData(ent, TuringDispenserVisuals.Status, status);
    }

    public bool TryGetBufferSolution(Entity<RMCTuringDispenserComponent> ent, out Entity<SolutionComponent>? solnEnt, out Solution? solution)
    {
        return _solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolution, out solnEnt, out solution);
    }

    public bool IsReadyForCentrifuge(Entity<RMCTuringDispenserComponent> ent)
    {
        return ent.Comp.OutputMode == TuringDispenserOutputMode.Centrifuge &&
               (ent.Comp.MemoryProgram.Count > 0 || ent.Comp.BoxProgram.Count > 0);
    }

    public void RequestRun(Entity<RMCTuringDispenserComponent> ent)
    {
        StartProgram(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var dispensers = EntityQueryEnumerator<RMCTuringDispenserComponent>();
        while (dispensers.MoveNext(out var uid, out var comp))
        {
            UpdateDispenser((uid, comp), time);
        }
    }

    private void UpdateDispenser(Entity<RMCTuringDispenserComponent> ent, TimeSpan time)
    {
        var comp = ent.Comp;

        if (time >= comp.NextRecharge)
        {
            comp.NextRecharge = time + comp.RechargeEvery;
            comp.Energy = FixedPoint2.Min(comp.MaxEnergy, comp.Energy + comp.RechargeAmount);
            Dirty(ent);
        }

        if (comp.Status is TuringDispenserStatus.Idle or TuringDispenserStatus.Finished)
            return;

        if (comp.Status == TuringDispenserStatus.Stuck)
        {
            if (time < comp.NextStuckRetry)
                return;

            SetStatus(ent, TuringDispenserStatus.Running);
        }

        if (time < comp.NextProcess)
            return;

        comp.NextProcess = time + comp.ProcessEvery;

        if (!TryGetTargetSolution(ent, out var targetSolnEnt, out var targetSolution))
        {
            SetStatus(ent, TuringDispenserStatus.Finished);
            return;
        }

        var guard = 0;
        var maxSteps = Math.Max(1, comp.MemoryProgram.Count + comp.BoxProgram.Count);
        while (guard++ < maxSteps)
        {
            if (comp.Cycle >= comp.CycleLimit)
            {
                SetStatus(ent, TuringDispenserStatus.Finished);
                return;
            }

            if (targetSolution!.AvailableVolume <= FixedPoint2.Zero)
            {
                SetStatus(ent, TuringDispenserStatus.Finished);
                return;
            }

            var program = comp.ActiveProgram == TuringDispenserProgram.Memory ? comp.MemoryProgram : comp.BoxProgram;
            if (program.Count == 0)
            {
                SetStatus(ent, TuringDispenserStatus.Finished);
                return;
            }

            if (comp.Stage >= program.Count)
            {
                AdvanceProgram(ent);
                continue;
            }

            var entry = program[comp.Stage];
            var amount = comp.StageMissing > FixedPoint2.Zero ? comp.StageMissing : entry.Amount * comp.Multiplier;
            amount = FixedPoint2.Min(amount, targetSolution.AvailableVolume);

            if (amount <= FixedPoint2.Zero)
            {
                comp.Stage++;
                comp.StageMissing = FixedPoint2.Zero;
                continue;
            }

            var fulfilled = FixedPoint2.Zero;
            if (comp.SmartLink)
            {
                var coords = Transform(ent).Coordinates;

                if (_smartFridge.TryDrainStock(coords, comp.SmartFridgeRange, entry.Reagent, amount, out var drained, comp.FlushedContainers) &&
                    drained > FixedPoint2.Zero)
                {
                    _solution.TryAddReagent(targetSolnEnt!.Value, entry.Reagent, drained);
                    fulfilled += drained;
                }

                if (fulfilled < amount &&
                    _cmRefillable.TryDrainRefiller(coords, comp.SmartFridgeRange, entry.Reagent, amount - fulfilled, out var refilled) &&
                    refilled > FixedPoint2.Zero)
                {
                    _solution.TryAddReagent(targetSolnEnt!.Value, entry.Reagent, refilled);
                    fulfilled += refilled;
                }
            }

            if (fulfilled < amount)
            {
                var remaining = amount - fulfilled;
                if (comp.SynthesizableReagents.Contains(entry.Reagent))
                {
                    var cost = remaining * comp.CostPerUnit;
                    if (comp.Energy < cost)
                    {
                        comp.StageMissing = fulfilled > FixedPoint2.Zero ? remaining : FixedPoint2.Zero;
                        Dirty(ent);
                        return;
                    }

                    comp.Energy -= cost;
                    _solution.TryAddReagent(targetSolnEnt!.Value, entry.Reagent, remaining);
                    fulfilled += remaining;
                }
                else if (fulfilled <= FixedPoint2.Zero)
                {
                    comp.Status = TuringDispenserStatus.Stuck;
                    comp.Error = $"{entry.Reagent.Id} NOT FOUND";
                    comp.NextStuckRetry = time + comp.StuckRetryDelay;
                    Dirty(ent);
                    return;
                }
            }

            comp.Stage++;
            comp.StageMissing = FixedPoint2.Zero;
            Dirty(ent);
        }
    }
}
