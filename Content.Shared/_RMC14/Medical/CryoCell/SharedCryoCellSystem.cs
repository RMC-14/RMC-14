using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Storage;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CryoCell;

public abstract class SharedCryoCellSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly RMCMovementSystem _rmcMovement = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnCryoCellInit);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnCryoCellEntInserted);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnCryoCellEntRemoved);

        SubscribeLocalEvent<InsideCryoCellComponent, MoveInputEvent>(OnInsideCryoCellMoveInput);
    }

    private void OnCryoCellInit(Entity<CryoCellComponent> cryoCell, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(cryoCell, cryoCell.Comp.OccupantId);
        UpdateCryoCellVisuals(cryoCell);
    }

    private void OnCryoCellEntInserted(Entity<CryoCellComponent> cryoCell, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != cryoCell.Comp.OccupantId)
            return;

        cryoCell.Comp.Occupant = args.Entity;

        Dirty(cryoCell);
        UpdateCryoCellVisuals(cryoCell);

        if (!_timing.ApplyingState)
            EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cryoCell;
    }

    private void OnCryoCellEntRemoved(Entity<CryoCellComponent> cryoCell, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != cryoCell.Comp.OccupantId)
            return;

        if (cryoCell.Comp.Occupant == args.Entity)
        {
            cryoCell.Comp.Occupant = null;
            Dirty(cryoCell);
        }

        UpdateCryoCellVisuals(cryoCell);
        RemCompDeferred<InsideCryoCellComponent>(args.Entity);
        _rmcMovement.SuppressCollisionOnExit(args.Entity, cryoCell.Owner);
    }

    private void OnInsideCryoCellMoveInput(Entity<InsideCryoCellComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Chamber is not { } cellId)
            return;

        if (!TryComp<CryoCellComponent>(cellId, out var cellComp))
            return;

        EjectOccupant((cellId, cellComp), ent);
    }

    protected void EjectOccupant(Entity<CryoCellComponent> cryoCell, EntityUid occupant)
    {
        if (!_container.TryGetContainer(cryoCell, cryoCell.Comp.OccupantId, out var container))
            return;

        _container.Remove(occupant, container);
        cryoCell.Comp.IsPoweredOn = false;

        if (cryoCell.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(cryoCell))
            _stun.TryStun(occupant, cryoCell.Comp.ExitStun, true);

        Dirty(cryoCell);
        UpdateCryoCellVisuals(cryoCell);
    }

    protected bool TryGetBeaker(
        Entity<CryoCellComponent> cryoCell,
        [NotNullWhen(true)] out ItemSlot? slot,
        out Entity<SolutionComponent> solution)
    {
        solution = default;
        if (!_itemSlots.TryGetSlot(cryoCell, cryoCell.Comp.BeakerSlot, out slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } contained)
        {
            return false;
        }

        if (!TryComp(contained, out FitsInDispenserComponent? fits))
            return false;

        if (!_solution.TryGetSolution(contained, fits.Solution, out var solutionNullable))
            return false;

        solution = solutionNullable.Value;
        return true;
    }

    protected void UpdateCryoCellVisuals(Entity<CryoCellComponent> cryoCell, bool? powered = null)
    {
        var isOn = cryoCell.Comp.IsPoweredOn && (powered ?? true);
        var hasOccupant = cryoCell.Comp.Occupant != null;

        if (_light.TryGetLight(cryoCell.Owner, out var light))
        {
            _light.SetEnabled(cryoCell.Owner, isOn, light);
        }

        var newState = (isOn, hasOccupant) switch
        {
            (true, false) => CryoCellVisualState.OnEmpty,
            (true, true) => CryoCellVisualState.OnOccupied,
            (false, false) => CryoCellVisualState.OffEmpty,
            (false, true) => CryoCellVisualState.OffOccupied,
        };

        if (_appearance.TryGetData<CryoCellVisualState>(cryoCell.Owner, CryoCellVisuals.State, out var oldState))
        {
            if (oldState == newState)
                return;
        }

        _appearance.SetData(cryoCell, CryoCellVisuals.State, newState);
    }
}
