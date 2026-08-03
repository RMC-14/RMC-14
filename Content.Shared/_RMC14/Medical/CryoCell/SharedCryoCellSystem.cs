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

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnCryoInit);
        SubscribeLocalEvent<CryoCellComponent, ComponentShutdown>(OnCryoShutdown);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnCryoEntInserted);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnCryoEntRemoved);
        SubscribeLocalEvent<CryoCellComponent, InteractHandEvent>(OnCryoInteractHand);

        SubscribeLocalEvent<InsideCryoCellComponent, MoveInputEvent>(OnInsideCryoMoveInput);
    }

    private void OnCryoInit(Entity<CryoCellComponent> cryo, ref ComponentInit args)
    {
        // Prototype-driven containers: ContainerContainer / ItemSlots defined in prototype.
        // Do not call EnsureContainer here.
    }

    private void OnCryoShutdown(Entity<CryoCellComponent> cryo, ref ComponentShutdown args)
    {
        // No console or other ephemeral dependencies to clean in shared code.
    }

    private void OnCryoEntInserted(Entity<CryoCellComponent> cryo, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == cryo.Comp.ContainerId)
        {
            cryo.Comp.Occupant = args.Entity;
            if (_net.IsServer)
                _audio.PlayPvs(cryo.Comp.InsertSound, cryo);
            Dirty(cryo);
            UpdateCryoVisuals(cryo);

            if (!_timing.ApplyingState)
                EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cryo;
        }
        else if (args.Container.ID == cryo.Comp.BeakerSlot)
        {
            // Beaker placed in beaker slot. Server may react to this.
        }
    }

    private void OnCryoEntRemoved(Entity<CryoCellComponent> cryo, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == cryo.Comp.ContainerId)
        {
            if (cryo.Comp.Occupant == args.Entity)
            {
                cryo.Comp.Occupant = null;
                Dirty(cryo);
            }

            UpdateCryoVisuals(cryo);
            RemCompDeferred<InsideCryoCellComponent>(args.Entity);
            _rmcMovement.SuppressCollisionOnExit(args.Entity, cryo.Owner);
        }
        else if (args.Container.ID == cryo.Comp.BeakerSlot)
        {
            // Beaker removed: server may react.
        }
    }

    private void OnCryoInteractHand(Entity<CryoCellComponent> cryo, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (cryo.Comp.Occupant is { } occupant)
        {
            EjectOccupant(cryo, occupant);
            args.Handled = true;
        }
    }

    protected void EjectOccupant(Entity<CryoCellComponent> cryo, EntityUid occupant)
    {
        if (!_container.TryGetContainer(cryo, cryo.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);

        if (cryo.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(cryo))
            _stun.TryStun(occupant, cryo.Comp.ExitStun, true);

        if (_net.IsClient)
            return;

        if (cryo.Comp.On && cryo.Comp.AutoEject && cryo.Comp.ReleaseNotice)
            _popup.PopupEntity(Loc.GetString("rmc-cryocell-ejected", ("entity", occupant)), cryo);

        _audio.PlayPvs(cryo.Comp.EjectSound, cryo);
    }

    protected bool TryGetBeaker(EntityUid uid, CryoCellComponent comp, out EntityUid beaker)
    {
        beaker = default;
        if (!_container.TryGetContainer(uid, comp.BeakerSlot, out var cont) || cont.ContainedEntities.Count == 0)
            return false;

        beaker = cont.ContainedEntities.First();
        return true;
    }

    private void UpdateCryoVisuals(Entity<CryoCellComponent> cryo)
    {
        var occupied = cryo.Comp.Occupant != null;
        // Consider adding a CryoVisuals enum in shared for proper visuals similar to Sleeper/BodyScanner.
        _appearance.SetData(cryo, /* placeholder index */ (byte)0, occupied);
    }

    private void OnInsideCryoMoveInput(Entity<InsideCryoCellComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Chamber is not { } cryoId)
            return;

        if (!TryComp<CryoCellComponent>(cryoId, out var cryo))
            return;

        EjectOccupant((cryoId, cryo), ent);
    }
}
