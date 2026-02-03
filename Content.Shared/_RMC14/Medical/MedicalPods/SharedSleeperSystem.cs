using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Storage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.MedicalPods;

public abstract class SharedSleeperSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleeperComponent, ComponentInit>(OnSleeperInit);
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
            sleeper.Comp.Filtering = false;
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
        // Link to nearby sleeper
        TryLinkToSleeper(console);
    }

    private void OnConsoleActivate(Entity<SleeperConsoleComponent> console, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (console.Comp.SkillRequired != null && !_skills.HasSkills(args.User, console.Comp.SkillRequired))
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

        // Search adjacent tiles for a sleeper
        var sleepers = new HashSet<Entity<SleeperComponent>>();
        _lookup.GetEntitiesInRange(coords, 1.5f, sleepers);

        foreach (var sleeper in sleepers)
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

        if (sleeper.Comp.EntryDelay > TimeSpan.Zero)
        {
            var doAfter = new DoAfterArgs(EntityManager, user, sleeper.Comp.EntryDelay, new SleeperEntryDoAfterEvent(), sleeper, sleeper)
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

        if (sleeper.Comp.PushInDelay > TimeSpan.Zero)
        {
            var doAfter = new DoAfterArgs(EntityManager, user, sleeper.Comp.PushInDelay, new SleeperPushInDoAfterEvent(), sleeper, target)
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

    public void InsertOccupant(Entity<SleeperComponent> sleeper, EntityUid occupant)
    {
        if (!_container.TryGetContainer(sleeper, sleeper.Comp.ContainerId, out var container))
            return;

        _container.Insert(occupant, container);
    }

    public void TryEjectOccupant(Entity<SleeperComponent> sleeper, EntityUid? user)
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

    public void EjectOccupant(Entity<SleeperComponent> sleeper, EntityUid occupant)
    {
        if (!_container.TryGetContainer(sleeper, sleeper.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);

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
    }

    public void ToggleDialysis(Entity<SleeperComponent> sleeper)
    {
        if (sleeper.Comp.Occupant == null)
        {
            sleeper.Comp.Filtering = false;
            sleeper.Comp.DialysisStartedReagentVolume = 0;
            return;
        }

        sleeper.Comp.Filtering = !sleeper.Comp.Filtering;
        if (sleeper.Comp.Filtering)
        {
            sleeper.Comp.NextDialysisTick = Timing.CurTime + sleeper.Comp.DialysisTickDelay;
        }
        else
        {
            sleeper.Comp.DialysisStartedReagentVolume = 0;
        }

        Dirty(sleeper);
    }
}
