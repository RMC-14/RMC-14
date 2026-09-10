using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Chemistry.TuringDispenser;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Centrifuge;

public sealed class SharedRMCCentrifugeSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedRMCTuringDispenserSystem _turing = default!;

    private readonly HashSet<Entity<RMCCentrifugeComponent>> _centrifuges = new();
    private readonly HashSet<Entity<RMCTuringDispenserComponent>> _turings = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCCentrifugeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCCentrifugeComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<RMCCentrifugeComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<RMCCentrifugeComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);

        Subs.BuiEvents<RMCCentrifugeComponent>(RMCCentrifugeUi.Key,
            subs =>
            {
                subs.Event<RMCCentrifugeToggleModeBuiMsg>(OnToggleModeMsg);
                subs.Event<RMCCentrifugeToggleSourceBuiMsg>(OnToggleSourceMsg);
                subs.Event<RMCCentrifugeSetLabelBuiMsg>(OnSetLabelMsg);
                subs.Event<RMCCentrifugeAttemptConnectionBuiMsg>(OnAttemptConnectionMsg);
                subs.Event<RMCCentrifugeEjectInputBuiMsg>(OnEjectInputMsg);
                subs.Event<RMCCentrifugeEjectOutputBuiMsg>(OnEjectOutputMsg);
            });
    }

    private void OnMapInit(Entity<RMCCentrifugeComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.InputSlotId);
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.OutputBoxSlotId);
        ConnectTuring(ent);
        UpdateAppearance(ent);
    }

    private void ConnectTuring(Entity<RMCCentrifugeComponent> ent)
    {
        if (ent.Comp.TuringDispenser != null)
            return;

        _turings.Clear();
        _entityLookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.TetherRange, _turings);
        foreach (var turing in _turings)
        {
            ent.Comp.TuringDispenser = turing.Owner;
            Dirty(ent);
            _popup.PopupClient("The centrifuge beeps: Turing Dispenser connected.", ent, PopupType.Medium);
            return;
        }
    }

    public bool HasNearbyCentrifuge(EntityCoordinates coords, float range)
    {
        _centrifuges.Clear();
        _entityLookup.GetEntitiesInRange(coords, range, _centrifuges);
        return _centrifuges.Count > 0;
    }

    private bool TuringReady(Entity<RMCCentrifugeComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.TuringDispenser is not { } turingUid ||
            !TryComp(turingUid, out RMCTuringDispenserComponent? turingComp))
        {
            comp.TuringDispenser = null;
            return false;
        }

        if (!Transform(ent).Coordinates.TryDistance(EntityManager, Transform(turingUid).Coordinates, out var dist) ||
            dist > comp.TetherRange)
        {
            comp.TuringDispenser = null;
            Dirty(ent);
            _popup.PopupClient("The centrifuge beeps: Turing not found within range.", ent, PopupType.Medium);
            return false;
        }

        return _turing.IsReadyForCentrifuge((turingUid, turingComp));
    }

    private void OnInsertAttempt(Entity<RMCCentrifugeComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var comp = ent.Comp;
        var slotId = args.Slot.ContainerSlot?.ID;

        if (comp.Spinning)
        {
            args.Cancelled = true;
            return;
        }

        if (slotId == comp.InputSlotId && comp.InputSource == CentrifugeInputSource.Turing)
            args.Cancelled = true;
    }

    private void OnEntInserted(Entity<RMCCentrifugeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.InputSlotId && args.Container.ID != ent.Comp.OutputBoxSlotId)
            return;

        UpdateAppearance(ent);

        if (!ent.Comp.Spinning && ent.Comp.InputSource == CentrifugeInputSource.Container)
            TryStartSpin(ent);
    }

    private void OnEntRemoved(Entity<RMCCentrifugeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.InputSlotId && args.Container.ID != ent.Comp.OutputBoxSlotId)
            return;

        UpdateAppearance(ent);
    }

    private void OnToggleModeMsg(Entity<RMCCentrifugeComponent> ent, ref RMCCentrifugeToggleModeBuiMsg args)
    {
        ent.Comp.Mode = ent.Comp.Mode == CentrifugeMode.Split ? CentrifugeMode.Distribute : CentrifugeMode.Split;
        Dirty(ent);
    }

    private void OnToggleSourceMsg(Entity<RMCCentrifugeComponent> ent, ref RMCCentrifugeToggleSourceBuiMsg args)
    {
        var comp = ent.Comp;
        if (comp.Spinning)
            return;

        comp.InputSource = comp.InputSource == CentrifugeInputSource.Container
            ? (comp.TuringDispenser != null ? CentrifugeInputSource.Turing : CentrifugeInputSource.Container)
            : CentrifugeInputSource.Container;
        Dirty(ent);
    }

    private void OnSetLabelMsg(Entity<RMCCentrifugeComponent> ent, ref RMCCentrifugeSetLabelBuiMsg args)
    {
        var label = args.Label.Trim();
        ent.Comp.Label = label.Length == 0 ? null : label[..Math.Min(label.Length, 32)];
        Dirty(ent);
    }

    private void OnAttemptConnectionMsg(Entity<RMCCentrifugeComponent> ent, ref RMCCentrifugeAttemptConnectionBuiMsg args)
    {
        ConnectTuring(ent);
    }

    private void OnEjectInputMsg(Entity<RMCCentrifugeComponent> ent, ref RMCCentrifugeEjectInputBuiMsg args)
    {
        if (ent.Comp.Spinning || !_itemSlots.TryGetSlot(ent, ent.Comp.InputSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);
    }

    private void OnEjectOutputMsg(Entity<RMCCentrifugeComponent> ent, ref RMCCentrifugeEjectOutputBuiMsg args)
    {
        if (ent.Comp.Spinning || !_itemSlots.TryGetSlot(ent, ent.Comp.OutputBoxSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);
    }

    private void TryStartSpin(Entity<RMCCentrifugeComponent> ent)
    {
        var comp = ent.Comp;

        if (!_itemSlots.TryGetSlot(ent, comp.OutputBoxSlotId, out var outputSlot) ||
            outputSlot.ContainerSlot?.ContainedEntity is null)
        {
            return;
        }

        if (comp.InputSource == CentrifugeInputSource.Container)
        {
            if (!_itemSlots.TryGetSlot(ent, comp.InputSlotId, out var inputSlot) ||
                inputSlot.ContainerSlot?.ContainedEntity is null)
            {
                return;
            }
        }

        comp.Spinning = true;
        comp.FinishAt = _timing.CurTime + comp.SpinDuration;
        Dirty(ent);
        UpdateAppearance(ent);
        _audio.PlayPvs(comp.SpinSound, ent);
    }

    private void UpdateAppearance(Entity<RMCCentrifugeComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.Spinning)
        {
            _appearance.SetData(ent, CentrifugeVisuals.State, CentrifugeVisualState.Spinning);
            return;
        }

        var hasInput = _itemSlots.TryGetSlot(ent, comp.InputSlotId, out var inputSlot) &&
                       inputSlot.ContainerSlot?.ContainedEntity != null;
        var hasOutput = _itemSlots.TryGetSlot(ent, comp.OutputBoxSlotId, out var outputSlot) &&
                         outputSlot.ContainerSlot?.ContainedEntity != null;

        var state = (hasInput, hasOutput) switch
        {
            (false, false) => CentrifugeVisualState.EmptyOpen,
            (false, true) => CentrifugeVisualState.EmptyClosed,
            (true, false) => CentrifugeVisualState.OnOpen,
            (true, true) => CentrifugeVisualState.OnClosed,
        };

        _appearance.SetData(ent, CentrifugeVisuals.State, state);
    }

    private void DoCentrifuge(Entity<RMCCentrifugeComponent> ent)
    {
        var comp = ent.Comp;

        Entity<SolutionComponent>? sourceSolnEnt;
        Solution? sourceSolution;

        if (comp.InputSource == CentrifugeInputSource.Turing &&
            comp.TuringDispenser is { } turingUid &&
            TryComp(turingUid, out RMCTuringDispenserComponent? turingComp))
        {
            _turing.TryGetBufferSolution((turingUid, turingComp), out sourceSolnEnt, out sourceSolution);
        }
        else if (_itemSlots.TryGetSlot(ent, comp.InputSlotId, out var inputSlot) &&
                 inputSlot.ContainerSlot?.ContainedEntity is { } beaker)
        {
            _solution.TryGetMixableSolution(beaker, out sourceSolnEnt, out sourceSolution);
        }
        else
        {
            sourceSolnEnt = null;
            sourceSolution = null;
        }

        if (sourceSolnEnt is null || sourceSolution is null || sourceSolution.Volume <= FixedPoint2.Zero)
        {
            comp.Spinning = false;
            Dirty(ent);
            _appearance.SetData(ent, CentrifugeVisuals.State, CentrifugeVisualState.Finish);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent, comp.OutputBoxSlotId, out var outputSlot) ||
            outputSlot.ContainerSlot?.ContainedEntity is not { } box ||
            !TryComp(box, out StorageComponent? storage))
        {
            comp.Spinning = false;
            Dirty(ent);
            UpdateAppearance(ent);
            return;
        }

        var vials = storage.Container.ContainedEntities.ToList();
        if (vials.Count > 0)
        {
            if (comp.Mode == CentrifugeMode.Split && sourceSolution.Contents.Count > 1)
                Split(sourceSolnEnt.Value, sourceSolution, vials);
            else
                Distribute(sourceSolution, vials);

            LabelVials(ent, vials);
        }

        comp.Spinning = false;
        Dirty(ent);
        _appearance.SetData(ent, CentrifugeVisuals.State, CentrifugeVisualState.Finish);
    }

    private void Split(Entity<SolutionComponent> source, Solution sourceSolution, List<EntityUid> vials)
    {
        foreach (var content in sourceSolution.Contents.ToArray())
        {
            var reagent = content.Reagent;

            var filtered = false;
            foreach (var vial in vials)
            {
                if (!_solution.TryGetSolution(vial, "beaker", out _, out var vialSolution))
                    continue;

                if (!vialSolution.ContainsReagent(reagent))
                    continue;

                if (vialSolution.Contents.Count > 1 || vialSolution.Volume >= vialSolution.MaxVolume)
                {
                    filtered = true;
                    break;
                }
            }

            if (filtered)
                continue;

            foreach (var vial in vials)
            {
                if (!_solution.TryGetSolution(vial, "beaker", out var vialSolnEnt, out var vialSolution))
                    continue;

                if (vialSolution.Contents.Count > 1)
                    continue;

                if (vialSolution.Contents.Count == 1 && !vialSolution.ContainsReagent(reagent))
                    continue;

                var available = vialSolution.AvailableVolume;
                if (available <= FixedPoint2.Zero)
                    continue;

                var amount = FixedPoint2.Min(available, sourceSolution.GetReagentQuantity(reagent));
                if (amount <= FixedPoint2.Zero)
                    continue;

                _solution.RemoveReagent(source, reagent, amount);
                _solution.TryAddReagent(vialSolnEnt!.Value, reagent.Prototype, amount, data: reagent.Data);
            }
        }
    }

    private void Distribute(Solution sourceSolution, List<EntityUid> vials)
    {
        foreach (var vial in vials)
        {
            if (sourceSolution.Volume <= FixedPoint2.Zero)
                break;

            if (!_solution.TryGetSolution(vial, "beaker", out var vialSolnEnt, out var vialSolution))
                continue;

            var available = vialSolution.AvailableVolume;
            if (available <= FixedPoint2.Zero)
                continue;

            var amount = FixedPoint2.Min(available, sourceSolution.Volume);
            _solution.TryTransferSolution(vialSolnEnt!.Value, sourceSolution, amount);
        }
    }

    private void LabelVials(Entity<RMCCentrifugeComponent> ent, List<EntityUid> vials)
    {
        foreach (var vial in vials)
        {
            if (!_solution.TryGetSolution(vial, "beaker", out _, out var solution))
                continue;

            string name;
            if (ent.Comp.Label is { } label)
                name = $"vial ({label})";
            else if (solution.Contents.Count == 1 && _rmcReagent.TryIndex(solution.Contents[0].Reagent.Prototype, out var reagent))
                name = $"vial ({reagent.LocalizedName})";
            else
                name = "vial";

            _metaData.SetEntityName(vial, name);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var centrifuges = EntityQueryEnumerator<RMCCentrifugeComponent>();
        while (centrifuges.MoveNext(out var uid, out var comp))
        {
            Entity<RMCCentrifugeComponent> ent = (uid, comp);

            if (comp.InputSource == CentrifugeInputSource.Turing && !comp.Spinning)
            {
                if (!TuringReady(ent) ||
                    comp.TuringDispenser is not { } turingUid ||
                    !TryComp(turingUid, out RMCTuringDispenserComponent? turingComp))
                {
                    continue;
                }

                if (_turing.TryGetBufferSolution((turingUid, turingComp), out _, out var buffer) &&
                    buffer != null && buffer.Volume > FixedPoint2.Zero)
                {
                    TryStartSpin(ent);
                }
                else
                {
                    if (turingComp.Status != TuringDispenserStatus.Running)
                        _audio.PlayPvs(comp.RequestRunSound, ent);

                    _turing.RequestRun((turingUid, turingComp));
                }

                continue;
            }

            if (comp.Spinning && time >= comp.FinishAt)
                DoCentrifuge(ent);
        }
    }
}
