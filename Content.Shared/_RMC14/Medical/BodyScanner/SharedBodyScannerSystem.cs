using Content.Shared._RMC14.Storage;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.BodyScanner;

public abstract class SharedBodyScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyScannerComponent, ComponentInit>(OnBodyScannerInit);
        SubscribeLocalEvent<BodyScannerComponent, MapInitEvent>(OnBodyScannerMapInit);
        SubscribeLocalEvent<BodyScannerComponent, ComponentShutdown>(OnBodyScannerShutdown);
        SubscribeLocalEvent<BodyScannerComponent, EntInsertedIntoContainerMessage>(OnBodyScannerEntInserted);
        SubscribeLocalEvent<BodyScannerComponent, EntRemovedFromContainerMessage>(OnBodyScannerEntRemoved);
        SubscribeLocalEvent<BodyScannerComponent, InteractHandEvent>(OnBodyScannerInteractHand);

        SubscribeLocalEvent<BodyScannerConsoleComponent, ActivateInWorldEvent>(OnConsoleActivateInWorld);

        SubscribeLocalEvent<InsideBodyScannerComponent, MoveInputEvent>(OnInsideBodyScannerMoveInput);
    }

    private void OnBodyScannerInit(Entity<BodyScannerComponent> scanner, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(scanner, scanner.Comp.ContainerId);
    }

    private void OnBodyScannerMapInit(Entity<BodyScannerComponent> scanner, ref MapInitEvent args)
    {
        if (_net.IsServer && scanner.Comp.SpawnConsolePrototype != null)
        {
            var rotation = Transform(scanner).LocalRotation;
            var rotatedOffset = rotation.RotateVec(scanner.Comp.ConsoleSpawnOffset);
            var consoleCoords = _transform.GetMoverCoordinates(scanner).Offset(rotatedOffset);
            var consoleId = Spawn(scanner.Comp.SpawnConsolePrototype.Value, consoleCoords);

            _transform.SetLocalRotation(consoleId, rotation);

            if (TryComp(consoleId, out BodyScannerConsoleComponent? console))
            {
                scanner.Comp.LinkedConsole = consoleId;
                console.LinkedBodyScanner = scanner;
                Dirty(scanner);
                Dirty(consoleId, console);
            }
        }
    }

    private void OnBodyScannerShutdown(Entity<BodyScannerComponent> scanner, ref ComponentShutdown args)
    {
        if (scanner.Comp.LinkedConsole is { } linkedConsoleId && TryComp(linkedConsoleId, out BodyScannerConsoleComponent? linkedConsole))
        {
            var spawnedByScanner = linkedConsole.LinkedBodyScanner == scanner.Owner;
            linkedConsole.LinkedBodyScanner = null;
            Dirty(linkedConsoleId, linkedConsole);

            if (_net.IsServer && spawnedByScanner)
                QueueDel(linkedConsoleId);
        }
    }

    private void OnBodyScannerEntInserted(Entity<BodyScannerComponent> scanner, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != scanner.Comp.ContainerId)
            return;

        scanner.Comp.Occupant = args.Entity;
        if (_net.IsServer)
            _audio.PlayPvs(scanner.Comp.InsertSound, scanner);
        Dirty(scanner);
        UpdateBodyScannerVisuals(scanner);

        if (!_timing.ApplyingState)
        {
            var inside = EnsureComp<InsideBodyScannerComponent>(args.Entity);
            inside.BodyScanner = scanner;
            Dirty(args.Entity, inside);
        }
    }

    private void OnBodyScannerEntRemoved(Entity<BodyScannerComponent> scanner, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != scanner.Comp.ContainerId)
            return;

        if (scanner.Comp.Occupant == args.Entity)
        {
            scanner.Comp.Occupant = null;
            Dirty(scanner);
        }

        UpdateBodyScannerVisuals(scanner);
        RemCompDeferred<InsideBodyScannerComponent>(args.Entity);
    }

    private void OnBodyScannerInteractHand(Entity<BodyScannerComponent> scanner, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (scanner.Comp.Occupant is { } occupant)
        {
            EjectOccupant(scanner, occupant);
            args.Handled = true;
        }
    }

    private void OnConsoleActivateInWorld(Entity<BodyScannerConsoleComponent> console, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        if (!VerifyConsoleOccupant(console, args.User))
            return;

        args.Handled = true;

        if (!TryGetLinkedScanner(console, out var scanner))
            return;

        if (scanner.Comp.Occupant is not { } occupant || TerminatingOrDeleted(occupant))
        {
            scanner.Comp.Occupant = null;
            return;
        }

        _audio.PlayPredicted(console.Comp.ScanSound, console, args.User);
        _popup.PopupClient(Loc.GetString("rmc-body-scanner-scan-stored", ("entity", occupant)), console, args.User);

        OnConsoleScan(console, occupant, args.User);
    }

    protected virtual void OnConsoleScan(Entity<BodyScannerConsoleComponent> console, EntityUid occupant, EntityUid user)
    {
    }

    protected bool TryGetLinkedScanner(Entity<BodyScannerConsoleComponent> console, out Entity<BodyScannerComponent> scanner)
    {
        scanner = default;
        if (console.Comp.LinkedBodyScanner is not { } linkedId || !TryComp(linkedId, out BodyScannerComponent? comp))
            return false;

        scanner = (linkedId, comp);
        return true;
    }

    private bool VerifyConsoleOccupant(Entity<BodyScannerConsoleComponent> console, EntityUid user)
    {
        if (console.Comp.LinkedBodyScanner is not { } scannerId || !TryComp<BodyScannerComponent>(scannerId, out var scanner))
        {
            _popup.PopupClient(Loc.GetString("rmc-body-scanner-no-scanner-connected"), console, user);
            return false;
        }

        if (scanner.Occupant is not { } occupant)
        {
            _popup.PopupClient(Loc.GetString("rmc-body-scanner-no-lifeform"), console, user);
            return false;
        }

        if (!HasComp<DamageableComponent>(occupant) || !HasComp<MobStateComponent>(occupant) || !HasComp<MobThresholdsComponent>(occupant))
        {
            _popup.PopupClient(Loc.GetString("rmc-body-scanner-incompatible-lifeform"), console, user);
            return false;
        }

        return true;
    }

    private void EjectOccupant(Entity<BodyScannerComponent> scanner, EntityUid occupant)
    {
        if (!_container.TryGetContainer(scanner, scanner.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);
        if (scanner.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(scanner))
            _stun.TryStun(occupant, scanner.Comp.ExitStun, true);

        if (!_net.IsServer)
            return;
        _audio.PlayPvs(scanner.Comp.EjectSound, scanner);
        _popup.PopupEntity(Loc.GetString("rmc-body-scanner-ejected", ("entity", occupant)), scanner);
    }

    private void UpdateBodyScannerVisuals(Entity<BodyScannerComponent> scanner)
    {
        var occupied = scanner.Comp.Occupant != null;
        _appearance.SetData(scanner, BodyScannerVisuals.Occupied, occupied);
    }

    private void OnInsideBodyScannerMoveInput(Entity<InsideBodyScannerComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.BodyScanner is not { } scannerId)
            return;

        if (!TryComp<BodyScannerComponent>(scannerId, out var scanner))
            return;

        EjectOccupant((scannerId, scanner), ent);
    }
}
