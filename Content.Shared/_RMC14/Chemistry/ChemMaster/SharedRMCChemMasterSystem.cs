using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.IconLabel;
using Content.Shared._RMC14.Storage;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

public abstract class SharedRMCChemMasterSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCIconLabelSystem _rmcIconLabel = default!;
    [Dependency] private readonly SharedRMCSmartFridgeSystem _rmcSmartFridge = default!;
    [Dependency] private readonly RMCStorageSystem _rmcStorage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    private readonly List<EntityUid> _toFill = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChemMasterComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCChemMasterComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<RMCChemMasterComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<RMCChemMasterComponent, RMCChemMasterPillBottleTransferDoAfterEvent>(OnPillBottleBoxTransferDoAfter);

        Subs.BuiEvents<RMCChemMasterComponent>(RMCChemMasterUI.Key,
            subs =>
            {
                subs.Event<RMCChemMasterPillBottleLabelMsg>(OnPillBottleLabelMsg);
                subs.Event<RMCChemMasterPillBottleColorMsg>(OnPillBottleColorMsg);
                subs.Event<RMCChemMasterPillBottleFillMsg>(OnPillBottleFillMsg);
                subs.Event<RMCChemMasterPillBottleTransferMsg>(OnPillBottleTransferMsg);
                subs.Event<RMCChemMasterPillBottleEjectMsg>(OnPillBottleEjectMsg);
                subs.Event<RMCChemMasterBeakerEjectMsg>(OnBeakerEjectMsg);
                subs.Event<RMCChemMasterBeakerTransferMsg>(OnBeakerTransferMsg);
                subs.Event<RMCChemMasterBeakerTransferAllMsg>(OnBeakerTransferAllMsg);
                subs.Event<RMCChemMasterBufferModeMsg>(OnBufferModeMsg);
                subs.Event<RMCChemMasterBufferTransferMsg>(OnBufferTransferMsg);
                subs.Event<RMCChemMasterBufferTransferAllMsg>(OnBufferTransferAllMsg);
                subs.Event<RMCChemMasterSetPillAmountMsg>(OnSetPillAmountMsg);
                subs.Event<RMCChemMasterSetPillTypeMsg>(OnSetPillTypeMsg);
                subs.Event<RMCChemMasterCreatePillsMsg>(OnCreatePillsMsg);
            });
    }

    private void OnInteractUsing(Entity<RMCChemMasterComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp(args.Used, out RMCPillBottleTransferComponent? boxComp) &&
            TryComp(args.Used, out StorageComponent? boxStorage))
        {
            args.Handled = true;
            var pillBottleSlot = _container.EnsureContainer<Container>(ent, ent.Comp.PillBottleContainer);
            var availableSpace = ent.Comp.MaxPillBottles - pillBottleSlot.Count;
            if (availableSpace <= 0)
            {
                _popup.PopupClient(Loc.GetString("rmc-chem-master-full-pill-bottles"), ent, args.User);
                return;
            }

            if (boxStorage.StoredItems.Count == 0)
            {
                _popup.PopupClient(Loc.GetString("rmc-chem-master-pill-bottle-box-empty", ("box", args.Used)), ent, args.User);
                return;
            }

            var bottlesToTransfer = Math.Min(boxStorage.StoredItems.Count, availableSpace);
            var transferTime = TimeSpan.FromSeconds(bottlesToTransfer * boxComp.TimePerBottle);
            var ev = new RMCChemMasterPillBottleTransferDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, transferTime, ev, ent.Owner, target: ent.Owner, used: args.Used)
            {
                BreakOnMove = true,
                NeedHand = true,
            };

            if (_doAfter.TryStartDoAfter(doAfterArgs))
            {
                _popup.PopupClient(Loc.GetString("rmc-chem-master-pill-bottle-box-start", ("box", args.Used), ("target", ent)), args.User, args.User);
            }
            return;
        }

        if (!_entityWhitelist.IsWhitelistPass(ent.Comp.PillBottleWhitelist, args.Used))
            return;

        args.Handled = true;
        var slot = _container.EnsureContainer<Container>(ent, ent.Comp.PillBottleContainer);
        if (slot.Count >= ent.Comp.MaxPillBottles)
        {
            _popup.PopupClient(Loc.GetString("rmc-chem-master-full-pill-bottles"), ent, args.User);
            return;
        }

        _container.Insert(args.Used, slot);
        _audio.PlayPredicted(ent.Comp.PillBottleInsertSound, ent, args.User);
    }

    protected virtual void OnEntInsertedIntoContainer(Entity<RMCChemMasterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BufferSolutionId)
            return;

        Dirty(ent);
    }

    protected virtual void OnEntRemovedFromContainer(Entity<RMCChemMasterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BufferSolutionId)
            return;

        ent.Comp.SelectedBottles.Remove(args.Entity);
        Dirty(ent);
    }

    private void OnPillBottleLabelMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterPillBottleLabelMsg args)
    {
        var label = args.Label;
        if (label.Length > ent.Comp.MaxLabelLength)
            label = label[..ent.Comp.MaxLabelLength];

        foreach (var bottle in ent.Comp.SelectedBottles)
        {
            if (!_container.TryGetContainingContainer((bottle, null), out var container) ||
                container.Owner != ent.Owner)
            {
                continue;
            }

            _label.Label(bottle, label);
            _rmcIconLabel.Label(bottle, "rmc-custom-container-label-text", ("customLabel", label));
        }

        Dirty(ent);
    }

    private void OnPillBottleColorMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterPillBottleColorMsg args)
    {
        foreach (var bottle in ent.Comp.SelectedBottles)
        {
            if (!_container.TryGetContainingContainer((bottle, null), out var container) ||
                container.Owner != ent.Owner)
            {
                continue;
            }

            _appearance.SetData(bottle, RMCPillBottleVisuals.Color, args.Color);
        }

        Dirty(ent);
    }

    private void OnPillBottleFillMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterPillBottleFillMsg args)
    {
        if (!TryGetEntity(args.Bottle, out var bottle))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.PillBottleContainer, out var container) ||
            !container.ContainedEntities.Contains(bottle.Value))
        {
            return;
        }

        if (args.Fill)
            ent.Comp.SelectedBottles.Add(bottle.Value);
        else
            ent.Comp.SelectedBottles.Remove(bottle.Value);

        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnPillBottleTransferMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterPillBottleTransferMsg args)
    {
        if (!TryGetEntity(args.Bottle, out var bottle))
            return;

        if (!_container.TryGetContainingContainer((bottle.Value, null), out var container) ||
            container.Owner != ent.Owner ||
            container.ID != ent.Comp.PillBottleContainer)
        {
            return;
        }

        _rmcSmartFridge.TransferToNearby(ent.Owner.ToCoordinates(), ent.Comp.LinkRange, bottle.Value);
        Dirty(ent);
    }

    private void OnPillBottleEjectMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterPillBottleEjectMsg args)
    {
        if (!TryGetEntity(args.Bottle, out var bottle))
            return;

        if (!_container.TryGetContainingContainer((bottle.Value, null), out var container) ||
            container.Owner != ent.Owner ||
            container.ID != ent.Comp.PillBottleContainer)
        {
            return;
        }

        if (_container.Remove(bottle.Value, container))
        {
            _hands.TryPickupAnyHand(args.Actor, bottle.Value);
            _audio.PlayPredicted(ent.Comp.PillBottleEjectSound, ent, args.Actor);
        }

        Dirty(ent);
    }

    private void OnBeakerEjectMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterBeakerEjectMsg args)
    {
        if (!TryGetBeaker(ent, out _, out var slot, out _))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor, true);

        Dirty(ent);
    }

    private void OnBeakerTransferMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterBeakerTransferMsg args)
    {
        if (args.Amount < FixedPoint2.Zero)
            return;

        if (!TryGetBeaker(ent, out _, out _, out var beakerSolution))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var buffer))
            return;

        var removed = beakerSolution.Comp.Solution.RemoveReagent(new ReagentQuantity(args.Reagent, args.Amount), true);

        _solution.TryAddReagent(buffer.Value, args.Reagent, removed, out var accepted);
        removed -= accepted;

        if (removed > FixedPoint2.Zero)
            _solution.TryAddReagent(beakerSolution, args.Reagent, removed);

        _solution.UpdateChemicals(buffer.Value);
        _solution.UpdateChemicals(beakerSolution);
        Dirty(ent);

        RefreshUIs(ent);
    }

    private void OnBeakerTransferAllMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterBeakerTransferAllMsg args)
    {
        if (!TryGetBeaker(ent, out var beaker, out _, out var beakerSolution))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var buffer))
            return;

        _solutionTransfer.Transfer(args.Actor, beaker, beakerSolution, ent, buffer.Value, beakerSolution.Comp.Solution.Volume);
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnBufferModeMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterBufferModeMsg args)
    {
        if (!Enum.IsDefined(args.Mode))
            return;

        ent.Comp.BufferTransferMode = args.Mode;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnBufferTransferMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterBufferTransferMsg args)
    {
        if (args.Amount < FixedPoint2.Zero)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var buffer))
            return;

        var removed = buffer.Value.Comp.Solution.RemoveReagent(new ReagentQuantity(args.Reagent, args.Amount), true);
        if (ent.Comp.BufferTransferMode == RMCChemMasterBufferMode.ToDisposal)
        {
            _solution.UpdateChemicals(buffer.Value);
            Dirty(ent);
            RefreshUIs(ent);
            return;
        }

        if (!TryGetBeaker(ent, out _, out _, out var beakerSolution))
            return;

        _solution.TryAddReagent(beakerSolution, args.Reagent, removed, out var accepted);
        removed -= accepted;

        if (removed > FixedPoint2.Zero)
            _solution.TryAddReagent(buffer.Value, args.Reagent, removed);

        _solution.UpdateChemicals(buffer.Value);
        _solution.UpdateChemicals(beakerSolution);
        Dirty(ent);

        RefreshUIs(ent);
    }

    private void OnBufferTransferAllMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterBufferTransferAllMsg args)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var buffer))
            return;

        if (ent.Comp.BufferTransferMode == RMCChemMasterBufferMode.ToDisposal)
        {
            _solution.RemoveAllSolution(buffer.Value);
        }
        else if (TryGetBeaker(ent, out var beaker, out _, out var beakerSolution))
        {
            _solutionTransfer.Transfer(args.Actor, ent, buffer.Value, beaker, beakerSolution, buffer.Value.Comp.Solution.Volume);
        }

        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnSetPillAmountMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterSetPillAmountMsg args)
    {
        ent.Comp.PillAmount = Math.Clamp(args.Amount, 1, ent.Comp.MaxPillAmount);
        Dirty(ent);
    }

    private void OnSetPillTypeMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterSetPillTypeMsg args)
    {
        if (args.Type <= 0 || args.Type > ent.Comp.PillTypes)
            return;

        ent.Comp.SelectedType = args.Type;
        Dirty(ent);
        RefreshUIs(ent);
    }

    private void OnCreatePillsMsg(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterCreatePillsMsg args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.PillBottleContainer, out var container))
            return;

        _toFill.Clear();
        foreach (var bottle in ent.Comp.SelectedBottles)
        {
            if (!container.Contains(bottle))
                continue;

            if (!TryComp(bottle, out StorageComponent? storage))
                continue;

            var free = _rmcStorage.EstimateFreeColumns((bottle, storage));
            if (free < ent.Comp.PillAmount)
            {
                var msg = Loc.GetString("rmc-chem-master-pills-not-enough-space");
                _popup.PopupClient(msg, args.Actor, PopupType.MediumCaution);
                return;
            }

            _toFill.Add(bottle);
        }

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var buffer))
        {
            var msg = Loc.GetString("rmc-chem-master-not-enough-space-solution");
            _popup.PopupClient(msg, args.Actor, PopupType.MediumCaution);
            return;
        }

        var solution = buffer.Value.Comp.Solution.Volume;
        var divider = (_toFill.Count * ent.Comp.PillAmount);
        if (divider == 0)
            return;

        var perPill = solution / divider;
        if (solution <= FixedPoint2.Zero || perPill <= FixedPoint2.Zero)
        {
            var msg = Loc.GetString("rmc-chem-master-not-enough-space-solution");
            _popup.PopupClient(msg, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (_net.IsClient)
            return;

        var originalSolution = string.Join(", ",
            buffer.Value.Comp.Solution.Contents.Select(c => $"{c.Quantity}u {c.Reagent.Prototype}"));
        var coords = Transform(ent).Coordinates;

        var reagentsPerPill = buffer.Value.Comp.Solution.Contents
            .Select(c => (c.Reagent.Prototype, Amount: c.Quantity / divider))
            .ToList();

        foreach (var fill in _toFill)
        {
            var label = CompOrNull<LabelComponent>(fill)?.CurrentLabel;
            for (var i = 0; i < ent.Comp.PillAmount; i++)
            {
                var pill = Spawn(ent.Comp.PillProto, coords);
                if (!_storage.Insert(fill, pill, out _, user: args.Actor, playSound: false))
                {
                    QueueDel(pill);
                    continue;
                }

                var pillComp = EnsureComp<PillComponent>(pill);
                pillComp.PillType = ent.Comp.SelectedType - 1;
                Dirty(pill, pillComp);

                if (label != null)
                    _label.Label(pill, label);

                if (TryComp(pill, out SolutionSpikerComponent? spiker) &&
                    _solution.TryGetSolution(pill, spiker.SourceSolution, out var pillSolution))
                {
                    foreach (var (reagentProto, amount) in reagentsPerPill)
                    {
                        var removed = buffer.Value.Comp.Solution.RemoveReagent(reagentProto, amount);
                        _solution.TryAddReagent(pillSolution.Value, reagentProto, removed);
                    }

                    _adminLog.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(args.Actor):player} transferred {SharedSolutionContainerSystem.ToPrettyString(pillSolution.Value.Comp.Solution)} to {ToPrettyString(pill):target}, which now contains {SharedSolutionContainerSystem.ToPrettyString(pillSolution.Value.Comp.Solution)}");
                }
            }
        }

        _solution.UpdateChemicals(buffer.Value);
        _adminLog.Add(LogType.RMCChemMaster,
            $"""
            {ToPrettyString(args.Actor):user} created {ent.Comp.PillAmount:pillAmount} {perPill:pillUnits}u pills in {ent.Comp.SelectedBottles.Count:bottleAmount} pill bottles using {ToPrettyString(ent):chemMaster}.
            Solution: {originalSolution:solution}
            Pill bottle IDs: {string.Join(", ", ent.Comp.SelectedBottles):bottleIds}
            """);

        Dirty(ent);
    }

    private void OnPillBottleBoxTransferDoAfter(Entity<RMCChemMasterComponent> ent, ref RMCChemMasterPillBottleTransferDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Used == null || !Exists(args.Used) ||
            !TryComp(args.Used, out RMCPillBottleTransferComponent? boxComp) ||
            !TryComp(args.Used, out StorageComponent? boxStorage))
        {
            _popup.PopupClient(Loc.GetString("rmc-chem-master-pill-bottle-box-failed"), args.User, args.User);
            return;
        }

        args.Handled = true;

        var slot = _container.EnsureContainer<Container>(ent, ent.Comp.PillBottleContainer);
        var availableSpace = ent.Comp.MaxPillBottles - slot.Count;
        if (availableSpace <= 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-chem-master-full-pill-bottles"), ent, args.User);
            return;
        }

        if (!_container.TryGetContainer(args.Used.Value, boxStorage.Container.ID, out var boxContainer))
            return;

        var transferred = 0;
        var bottlesToTransfer = boxContainer.ContainedEntities.ToList();
        foreach (var bottle in bottlesToTransfer)
        {
            if (transferred >= availableSpace)
                break;

            if (!Exists(bottle) ||
                !_entityWhitelist.IsWhitelistPass(ent.Comp.PillBottleWhitelist, bottle) ||
                !_container.Remove(bottle, boxContainer))
                continue;

            if (_container.Insert(bottle, slot))
            {
                transferred++;
            }
            else if (!_container.Insert(bottle, boxContainer))
            {
                _hands.TryPickupAnyHand(args.User, bottle);
            }
        }

        if (transferred > 0)
        {
            _audio.PlayPredicted(boxComp.InsertPillBottleSound, ent, args.User);
            _popup.PopupClient(Loc.GetString("rmc-chem-master-pill-bottle-box-complete", ("count", transferred), ("target", ent)), args.User, args.User);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-chem-master-pill-bottle-box-failed"), args.User, args.User);
        }

        Dirty(ent);
    }

    private bool TryGetBeaker(
        Entity<RMCChemMasterComponent> chemMaster,
        out Entity<FitsInDispenserComponent> beaker,
        [NotNullWhen(true)] out ItemSlot? slot,
        out Entity<SolutionComponent> solution)
    {
        beaker = default;
        solution = default;
        if (!_itemSlots.TryGetSlot(chemMaster, chemMaster.Comp.BeakerSlot, out slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } contained)
        {
            return false;
        }

        if (!TryComp(contained, out FitsInDispenserComponent? fits))
            return false;

        if (!_solution.TryGetSolution(contained, fits.Solution, out var solutionNullable))
            return false;

        beaker = (contained, fits);
        solution = solutionNullable.Value;
        return true;
    }

    protected virtual void RefreshUIs(Entity<RMCChemMasterComponent> ent)
    {
    }
}
