using Content.Shared._RMC14.Storage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.MedicalPods;

public abstract class SharedSleeperSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleeperComponent, ComponentInit>(OnSleeperInit);
        SubscribeLocalEvent<SleeperComponent, MapInitEvent>(OnSleeperMapInit);
        SubscribeLocalEvent<SleeperComponent, ComponentShutdown>(OnSleeperShutdown);
        SubscribeLocalEvent<SleeperComponent, EntInsertedIntoContainerMessage>(OnSleeperEntInserted);
        SubscribeLocalEvent<SleeperComponent, EntRemovedFromContainerMessage>(OnSleeperEntRemoved);
        SubscribeLocalEvent<SleeperComponent, InteractHandEvent>(OnSleeperInteractHand);
        SubscribeLocalEvent<SleeperComponent, GetVerbsEvent<InteractionVerb>>(OnSleeperVerbs);
        SubscribeLocalEvent<SleeperComponent, SleeperEntryDoAfterEvent>(OnSleeperEntryDoAfter);
        SubscribeLocalEvent<SleeperComponent, SleeperPushInDoAfterEvent>(OnSleeperPushInDoAfter);
        SubscribeLocalEvent<SleeperComponent, CanDropTargetEvent>(OnSleeperCanDrop);
        SubscribeLocalEvent<SleeperComponent, DragDropTargetEvent>(OnSleeperDragDrop);

        SubscribeLocalEvent<SleeperConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<SleeperConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
    }

    private void OnSleeperInit(Entity<SleeperComponent> sleeper, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(sleeper, sleeper.Comp.ContainerId);
    }

    private void OnSleeperMapInit(Entity<SleeperComponent> sleeper, ref MapInitEvent args)
    {
        // Spawn the console at offset position and link it to this sleeper
        if (_net.IsServer && sleeper.Comp.SpawnConsolePrototype != null)
        {
            var rotation = Transform(sleeper).LocalRotation;
            var rotatedOffset = rotation.RotateVec(sleeper.Comp.ConsoleSpawnOffset);
            var consoleCoords = _transform.GetMoverCoordinates(sleeper).Offset(rotatedOffset);
            var consoleId = Spawn(sleeper.Comp.SpawnConsolePrototype.Value, consoleCoords);

            // Set the console's rotation to match the sleeper
            _transform.SetLocalRotation(consoleId, rotation);

            if (TryComp(consoleId, out SleeperConsoleComponent? console))
            {
                sleeper.Comp.SpawnedConsole = consoleId;
                sleeper.Comp.LinkedConsole = consoleId;
                console.LinkedSleeper = sleeper;
                console.SpawnedBySleeper = sleeper;
                Dirty(sleeper);
                Dirty(consoleId, console);
            }
        }
    }

    private void OnSleeperShutdown(Entity<SleeperComponent> sleeper, ref ComponentShutdown args)
    {
        // Unlink the currently linked console (if any)
        if (sleeper.Comp.LinkedConsole is { } linkedConsoleId && TryComp(linkedConsoleId, out SleeperConsoleComponent? linkedConsole))
        {
            linkedConsole.LinkedSleeper = null;
            Dirty(linkedConsoleId, linkedConsole);
        }

        // Only delete the console that was SPAWNED by this sleeper
        if (_net.IsServer &&
            sleeper.Comp.SpawnedConsole is { } spawnedConsoleId &&
            TryComp(spawnedConsoleId, out SleeperConsoleComponent? spawnedConsole))
        {
            if (spawnedConsole.SpawnedBySleeper == sleeper.Owner)
            {
                QueueDel(spawnedConsoleId);
            }
        }
    }

    private void OnSleeperEntInserted(Entity<SleeperComponent> sleeper, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != sleeper.Comp.ContainerId)
            return;

        sleeper.Comp.Occupant = args.Entity;
        Dirty(sleeper);
        UpdateSleeperVisuals(sleeper);
    }

    private void OnSleeperEntRemoved(Entity<SleeperComponent> sleeper, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != sleeper.Comp.ContainerId)
            return;

        if (sleeper.Comp.Occupant == args.Entity)
        {
            sleeper.Comp.Occupant = null;
            sleeper.Comp.IsFiltering = false;
            sleeper.Comp.DialysisStartedReagentVolume = 0;
            Dirty(sleeper);
        }
        UpdateSleeperVisuals(sleeper);
    }

    private void OnSleeperInteractHand(Entity<SleeperComponent> sleeper, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (sleeper.Comp.Occupant != null)
        {
            TryEjectOccupant(sleeper, args.User);
            args.Handled = true;
        }
    }

    private void OnSleeperVerbs(Entity<SleeperComponent> sleeper, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (sleeper.Comp.Occupant != null)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Act = () => TryEjectOccupant(sleeper, user),
                Text = Loc.GetString("rmc-sleeper-eject-verb"),
            });
        }
        else if (args.Target != user)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Act = () => TryEnterSleeper(sleeper, user),
                Text = Loc.GetString("rmc-sleeper-enter-verb"),
            });
        }
    }

    private void OnSleeperEntryDoAfter(Entity<SleeperComponent> sleeper, ref SleeperEntryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), args.User, args.User);
            return;
        }

        InsertOccupant(sleeper, args.User);
    }

    private void OnSleeperPushInDoAfter(Entity<SleeperComponent> sleeper, ref SleeperPushInDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), args.User, args.User);
            return;
        }

        if (args.Target is not { } target)
            return;

        InsertOccupant(sleeper, target);
        _popup.PopupClient(Loc.GetString("rmc-sleeper-inserted-other", ("target", target), ("sleeper", sleeper)), args.User, args.User);
    }

    private static void OnSleeperCanDrop(Entity<SleeperComponent> sleeper, ref CanDropTargetEvent args)
    {
        if (sleeper.Comp.Occupant != null)
            return;

        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnSleeperDragDrop(Entity<SleeperComponent> sleeper, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), args.User, args.User);
            return;
        }

        if (args.Dragged == args.User)
        {
            TryEnterSleeper(sleeper, args.User);
        }
        else
        {
            TryPushIntoSleeper(sleeper, args.User, args.Dragged);
        }
    }

    private void OnConsoleShutdown(Entity<SleeperConsoleComponent> console, ref ComponentShutdown args)
    {
        // Clean up the sleeper's reference to this console
        if (console.Comp.LinkedSleeper is { } linkedSleeperId && TryComp(linkedSleeperId, out SleeperComponent? linkedSleeper))
        {
            if (linkedSleeper.LinkedConsole == console.Owner)
            {
                linkedSleeper.LinkedConsole = null;
                Dirty(linkedSleeperId, linkedSleeper);
            }

            // Also clear SpawnedConsole if this console was spawned by that sleeper
            if (linkedSleeper.SpawnedConsole == console.Owner)
            {
                linkedSleeper.SpawnedConsole = null;
                Dirty(linkedSleeperId, linkedSleeper);
            }
        }

        // Also check WasSpawnedBySleeper in case it's different from LinkedSleeper
        if (console.Comp.SpawnedBySleeper is { } spawnedBySleeperId &&
            spawnedBySleeperId != console.Comp.LinkedSleeper &&
            TryComp(spawnedBySleeperId, out SleeperComponent? spawnerSleeper))
        {
            if (spawnerSleeper.SpawnedConsole == console.Owner)
            {
                spawnerSleeper.SpawnedConsole = null;
                Dirty(spawnedBySleeperId, spawnerSleeper);
            }
        }
    }

    private void OnConsoleUIOpenAttempt(Entity<SleeperConsoleComponent> console, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (console.Comp.LinkedSleeper is not { } sleeperId || !HasComp<SleeperComponent>(sleeperId))
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-no-sleeper-connected"), console, args.User);
            args.Cancel();
        }
    }

    private void TryEnterSleeper(Entity<SleeperComponent> sleeper, EntityUid user)
    {
        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), user, user);
            return;
        }

        if (sleeper.Comp.InsertSelfDelay > TimeSpan.Zero)
        {
            var doAfter = new DoAfterArgs(EntityManager, user, sleeper.Comp.InsertSelfDelay, new SleeperEntryDoAfterEvent(), sleeper, sleeper)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
            };
            _doAfter.TryStartDoAfter(doAfter);
        }
        else
        {
            InsertOccupant(sleeper, user);
        }
    }

    private void TryPushIntoSleeper(Entity<SleeperComponent> sleeper, EntityUid user, EntityUid target)
    {
        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), user, user);
            return;
        }

        _popup.PopupClient(Loc.GetString("rmc-sleeper-inserting", ("target", target), ("sleeper", sleeper)), user, user);
        if (sleeper.Comp.InsertOthersDelay > TimeSpan.Zero)
        {
            var doAfter = new DoAfterArgs(EntityManager, user, sleeper.Comp.InsertOthersDelay, new SleeperPushInDoAfterEvent(), sleeper, target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
            };
            _doAfter.TryStartDoAfter(doAfter);
        }
        else
        {
            InsertOccupant(sleeper, target);
            _popup.PopupClient(Loc.GetString("rmc-sleeper-inserted-other", ("target", target), ("sleeper", sleeper)), user, user);
        }
    }

    private void InsertOccupant(Entity<SleeperComponent> sleeper, EntityUid occupant)
    {
        if (!_container.TryGetContainer(sleeper, sleeper.Comp.ContainerId, out var container))
            return;

        _container.Insert(occupant, container);
    }

    protected void TryEjectOccupant(Entity<SleeperComponent> sleeper, EntityUid? user)
    {
        if (sleeper.Comp.Occupant is not { } occupant)
            return;

        EjectOccupant(sleeper, occupant);
    }

    protected void EjectOccupant(Entity<SleeperComponent> sleeper, EntityUid occupant)
    {
        if (!_container.TryGetContainer(sleeper, sleeper.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);
        _audio.PlayPvs(sleeper.Comp.EjectSound, sleeper);

        if (sleeper.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(sleeper))
        {
            _stun.TryStun(occupant, sleeper.Comp.ExitStun, true);
        }

        if (_net.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("rmc-sleeper-ejected", ("entity", occupant)), sleeper);
        }
    }

    private void UpdateSleeperVisuals(Entity<SleeperComponent> sleeper)
    {
        var occupied = sleeper.Comp.Occupant != null;
        _appearance.SetData(sleeper, SleeperVisuals.Occupied, occupied);

        var occupantState = SleeperOccupantMobState.None;
        if (sleeper.Comp.Occupant is { } occupant)
        {
            if (_mobStateSystem.IsDead(occupant))
                occupantState = SleeperOccupantMobState.Dead;
            else if (_mobStateSystem.IsCritical(occupant))
                occupantState = SleeperOccupantMobState.Critical;
            else
                occupantState = SleeperOccupantMobState.Alive;
        }

        _appearance.SetData(sleeper, SleeperVisuals.OccupantHealthState, occupantState);
    }

    protected void ToggleDialysis(Entity<SleeperComponent> sleeper)
    {
        if (sleeper.Comp.Occupant == null)
        {
            sleeper.Comp.IsFiltering = false;
            sleeper.Comp.DialysisStartedReagentVolume = 0;
            return;
        }

        sleeper.Comp.IsFiltering = !sleeper.Comp.IsFiltering;
        if (sleeper.Comp.IsFiltering)
        {
            sleeper.Comp.NextDialysisTick = _timing.CurTime + sleeper.Comp.DialysisTickDelay;
        }
        else
        {
            sleeper.Comp.DialysisStartedReagentVolume = 0;
        }

        Dirty(sleeper);
    }
}
