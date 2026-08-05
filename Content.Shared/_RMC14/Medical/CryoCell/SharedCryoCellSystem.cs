using System.Linq;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Storage;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CryoCell;

public abstract class SharedCryoCellSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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
        _container.EnsureContainer<ContainerSlot>(cell, cell.Comp.ContainerId);
    }

    private void OnCryoCellEntInserted(Entity<CryoCellComponent> cell, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != cell.Comp.ContainerId)
            return;

        cell.Comp.Occupant = args.Entity;

        Dirty(cell);
        UpdateCryoCellVisuals(cell);

        if (!_timing.ApplyingState)
            EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cell;
    }

    private void OnCryoCellEntRemoved(Entity<CryoCellComponent> cell, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != cell.Comp.ContainerId)
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
        if (!_container.TryGetContainer(cell, cell.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);

        if (cell.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(cell))
            _stun.TryStun(occupant, cell.Comp.ExitStun, true);

        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-ejected", ("entity", occupant)), cell);
    }

    protected bool TryGetBeaker(EntityUid uid, CryoCellComponent comp, out EntityUid beaker)
    {
        beaker = default;
        if (!_container.TryGetContainer(uid, comp.BeakerSlot, out var cont) || cont.ContainedEntities.Count == 0)
            return false;

        beaker = cont.ContainedEntities.First();
        return true;
    }

    protected void UpdateCryoCellVisuals(Entity<CryoCellComponent> cell, bool? powered = null)
    {
        var isOn = cell.Comp.IsPoweredOn && (powered ?? true);
        var hasOccupant = cell.Comp.Occupant != null;

        var state = (isOn, hasOccupant) switch
        {
            (true, false) => CryoCellVisualState.OnEmpty,
            (true, true) => CryoCellVisualState.OnOccupied,
            (false, false) => CryoCellVisualState.OffEmpty,
            (false, true) => CryoCellVisualState.OffOccupied,
        };

        _appearance.SetData(cell, CryoCellVisuals.State, state);
    }
}
