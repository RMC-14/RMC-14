using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Storage;
using Content.Shared.Movement.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CryoCell;

public abstract class SharedCryoCellSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly RMCMovementSystem _rmcMovement = default!;
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

    private void OnCryoCellInit(Entity<CryoCellComponent> cell, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(cell, cell.Comp.OccupantId);
        UpdateCryoCellVisuals(cell);
    }

    private void OnCryoCellEntInserted(Entity<CryoCellComponent> cell, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != cell.Comp.OccupantId)
            return;

        cell.Comp.Occupant = args.Entity;

        Dirty(cell);
        UpdateCryoCellVisuals(cell);

        if (!_timing.ApplyingState)
            EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cell;
    }

    private void OnCryoCellEntRemoved(Entity<CryoCellComponent> cell, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != cell.Comp.OccupantId)
            return;

        if (cell.Comp.Occupant == args.Entity)
        {
            cell.Comp.Occupant = null;
            Dirty(cell);
        }

        UpdateCryoCellVisuals(cell);
        RemCompDeferred<InsideCryoCellComponent>(args.Entity);
        _rmcMovement.SuppressCollisionOnExit(args.Entity, cell.Owner);
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

    protected void EjectOccupant(Entity<CryoCellComponent> cell, EntityUid occupant)
    {
        if (!_container.TryGetContainer(cell, cell.Comp.OccupantId, out var container))
            return;

        _container.Remove(occupant, container);
        cell.Comp.IsPoweredOn = false;

        if (cell.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(cell))
            _stun.TryStun(occupant, cell.Comp.ExitStun, true);

        Dirty(cell);
        UpdateCryoCellVisuals(cell);
    }

    protected bool TryGetBeaker(Entity<CryoCellComponent> cell, out EntityUid beaker)
    {
        beaker = default;
        if (!_container.TryGetContainer(cell, cell.Comp.BeakerSlot, out var container))
            return false;

        if (container is not ContainerSlot { ContainedEntity: { } ent })
            return false;

        beaker = ent;
        return true;
    }

    protected void UpdateCryoCellVisuals(Entity<CryoCellComponent> cell, bool? powered = null)
    {
        var isOn = cell.Comp.IsPoweredOn && (powered ?? true);
        var hasOccupant = cell.Comp.Occupant != null;

        if (_light.TryGetLight(cell.Owner, out var light))
        {
            _light.SetEnabled(cell.Owner, isOn, light);
        }

        var newState = (isOn, hasOccupant) switch
        {
            (true, false) => CryoCellVisualState.OnEmpty,
            (true, true) => CryoCellVisualState.OnOccupied,
            (false, false) => CryoCellVisualState.OffEmpty,
            (false, true) => CryoCellVisualState.OffOccupied,
        };

        if (_appearance.TryGetData<CryoCellVisualState>(cell.Owner, CryoCellVisuals.State, out var oldState))
        {
            if (oldState == newState)
                return;
        }

        _appearance.SetData(cell, CryoCellVisuals.State, newState);
    }
}
