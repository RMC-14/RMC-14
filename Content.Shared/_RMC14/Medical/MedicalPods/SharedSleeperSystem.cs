using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Storage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.MedicalPods;

public abstract class SharedSleeperSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    private static readonly SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    private readonly HashSet<Entity<SleeperComponent>> _sleeperLinkBuffer = new();

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

        SubscribeLocalEvent<SleeperConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<SleeperConsoleComponent, ActivateInWorldEvent>(OnConsoleActivate);
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
            var xform = Transform(sleeper);
            var rotation = xform.LocalRotation;

            // Apply rotation to the offset so console spawns in the correct relative position
            var rotatedOffset = rotation.RotateVec(sleeper.Comp.ConsoleSpawnOffset);
            var consoleCoords = _transform.GetMoverCoordinates(sleeper).Offset(rotatedOffset);

            var consoleId = Spawn(sleeper.Comp.SpawnConsolePrototype.Value, consoleCoords);

            // Set the console's rotation to match the sleeper
            _transform.SetLocalRotation(consoleId, rotation);

            if (TryComp(consoleId, out SleeperConsoleComponent? console))
            {
                sleeper.Comp.LinkedConsole = consoleId;
                console.LinkedSleeper = sleeper;
                Dirty(sleeper);
                Dirty(consoleId, console);
            }
        }
    }

    private void OnSleeperShutdown(Entity<SleeperComponent> sleeper, ref ComponentShutdown args)
    {
        // Clean up linked console when sleeper is destroyed
        if (sleeper.Comp.LinkedConsole is { } consoleId && TryComp(consoleId, out SleeperConsoleComponent? console))
        {
            console.LinkedSleeper = null;
            Dirty(consoleId, console);

            // Optionally delete the console when the sleeper is destroyed
            if (_net.IsServer)
                QueueDel(consoleId);
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

    private void OnSleeperCanDrop(Entity<SleeperComponent> sleeper, ref CanDropTargetEvent args)
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

        var target = args.Dragged;
        var user = args.User;

        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), user, user);
            return;
        }

        if (target == user)
        {
            TryEnterSleeper(sleeper, user);
        }
        else
        {
            TryPushIntoSleeper(sleeper, user, target);
        }
    }

    private void OnConsoleInit(Entity<SleeperConsoleComponent> console, ref ComponentInit args)
    {
        // Fallback: Try to link to nearby sleeper if not already linked
        // This handles manually placed consoles on maps
        TryLinkToSleeper(console);
    }

    private void OnConsoleActivate(Entity<SleeperConsoleComponent> console, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (console.Comp.UseConsoleSkill != null && !_skills.HasSkills(args.User, console.Comp.UseConsoleSkill))
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-no-skill"), args.User, args.User);
            args.Handled = true;
        }
    }

    private void TryLinkToSleeper(Entity<SleeperConsoleComponent> console)
    {
        if (console.Comp.LinkedSleeper != null)
            return;

        var xform = Transform(console);
        var coords = xform.Coordinates;

        // Search adjacent tiles for a sleeper using reusable buffer
        _sleeperLinkBuffer.Clear();
        _lookup.GetEntitiesInRange(coords, 1.5f, _sleeperLinkBuffer);

        foreach (var sleeper in _sleeperLinkBuffer)
        {
            console.Comp.LinkedSleeper = sleeper;
            sleeper.Comp.LinkedConsole = console;
            Dirty(console);
            Dirty(sleeper);
            return;
        }
    }

    public void TryEnterSleeper(Entity<SleeperComponent> sleeper, EntityUid user)
    {
        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), user, user);
            return;
        }

        if (sleeper.Comp.SkillRequired != null && !_skills.HasSkills(user, sleeper.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-no-skill-entry"), user, user);
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

    public void TryPushIntoSleeper(Entity<SleeperComponent> sleeper, EntityUid user, EntityUid target)
    {
        if (sleeper.Comp.Occupant != null)
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-already-occupied", ("sleeper", sleeper)), user, user);
            return;
        }

        if (sleeper.Comp.SkillRequired != null && !_skills.HasSkills(user, sleeper.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-no-skill"), user, user);
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

    protected void InsertOccupant(Entity<SleeperComponent> sleeper, EntityUid occupant)
    {
        if (!_container.TryGetContainer(sleeper, sleeper.Comp.ContainerId, out var container))
            return;

        _container.Insert(occupant, container);
    }

    protected void TryEjectOccupant(Entity<SleeperComponent> sleeper, EntityUid? user)
    {
        if (sleeper.Comp.Occupant is not { } occupant)
            return;

        if (user != null && sleeper.Comp.SkillRequired != null && !_skills.HasSkills(user.Value, sleeper.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("rmc-sleeper-no-skill"), user.Value, user.Value);
            return;
        }

        EjectOccupant(sleeper, occupant);
    }

    protected void EjectOccupant(Entity<SleeperComponent> sleeper, EntityUid occupant)
    {
        if (!_container.TryGetContainer(sleeper, sleeper.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);
        _audio.PlayPvs(EjectSound, sleeper);

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
        Appearance.SetData(sleeper, SleeperVisuals.Occupied, occupied);

        var healthState = SleeperOccupantHealthState.Alive;
        if (sleeper.Comp.Occupant is { } occupant)
        {
            if (_mobStateSystem.IsDead(occupant))
                healthState = SleeperOccupantHealthState.Dead;
            else if (_mobStateSystem.IsCritical(occupant))
                healthState = SleeperOccupantHealthState.Critical;
        }

        Appearance.SetData(sleeper, SleeperVisuals.OccupantHealthState, healthState);
    }

    public void ToggleDialysis(Entity<SleeperComponent> sleeper)
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
