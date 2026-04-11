using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Storage;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Autodoc;

public abstract class SharedAutodocSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutodocComponent, ComponentInit>(OnAutodocInit);
        SubscribeLocalEvent<AutodocComponent, MapInitEvent>(OnAutodocMapInit);
        SubscribeLocalEvent<AutodocComponent, ComponentShutdown>(OnAutodocShutdown);
        SubscribeLocalEvent<AutodocComponent, EntInsertedIntoContainerMessage>(OnAutodocEntInserted);
        SubscribeLocalEvent<AutodocComponent, EntRemovedFromContainerMessage>(OnAutodocEntRemoved);
        SubscribeLocalEvent<AutodocComponent, InteractHandEvent>(OnAutodocInteractHand);
        SubscribeLocalEvent<AutodocComponent, CanDropTargetEvent>(OnAutodocCanDropTarget, before: [typeof(DragInsertContainerSystem)]);
        SubscribeLocalEvent<AutodocComponent, DragDropTargetEvent>(OnAutodocDragDropTarget, before: [typeof(DragInsertContainerSystem)]);

        SubscribeLocalEvent<AutodocConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
        SubscribeLocalEvent<AutodocConsoleComponent, InteractUsingEvent>(OnConsoleInteractUsing);

        SubscribeLocalEvent<InsideAutodocComponent, MoveInputEvent>(OnInsideAutodocMoveInput);
    }

    private void OnAutodocInit(Entity<AutodocComponent> autodoc, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(autodoc, autodoc.Comp.ContainerId);
    }

    private void OnAutodocMapInit(Entity<AutodocComponent> autodoc, ref MapInitEvent args)
    {
        // Spawn the console at offset position and link it to this autodoc
        if (_net.IsServer && autodoc.Comp.SpawnConsolePrototype != null)
        {
            var rotation = Transform(autodoc).LocalRotation;
            var rotatedOffset = rotation.RotateVec(autodoc.Comp.ConsoleSpawnOffset);
            var consoleCoords = _transform.GetMoverCoordinates(autodoc).Offset(rotatedOffset);
            var consoleId = Spawn(autodoc.Comp.SpawnConsolePrototype.Value, consoleCoords);

            // Set the console's rotation to match the autodoc + 180 degrees because sprites are opposite
            _transform.SetLocalRotation(consoleId, rotation + Math.PI);

            if (TryComp(consoleId, out AutodocConsoleComponent? console))
            {
                autodoc.Comp.LinkedConsole = consoleId;
                console.LinkedAutodoc = autodoc;
                Dirty(autodoc);
                Dirty(consoleId, console);
            }
        }
    }

    private void OnAutodocShutdown(Entity<AutodocComponent> autodoc, ref ComponentShutdown args)
    {
        // Clean up linked console
        if (autodoc.Comp.LinkedConsole is { } linkedConsoleId && TryComp(linkedConsoleId, out AutodocConsoleComponent? linkedConsole))
        {
            var spawnedByAutodoc = linkedConsole.LinkedAutodoc == autodoc.Owner;
            linkedConsole.LinkedAutodoc = null;
            Dirty(linkedConsoleId, linkedConsole);

            if (_net.IsServer && spawnedByAutodoc)
                QueueDel(linkedConsoleId);
        }
    }

    private void OnAutodocEntInserted(Entity<AutodocComponent> autodoc, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != autodoc.Comp.ContainerId)
            return;

        autodoc.Comp.Occupant = args.Entity;
        Dirty(autodoc);
        UpdateAutodocVisuals(autodoc);

        if (!_timing.ApplyingState)
        {
            var inside = EnsureComp<InsideAutodocComponent>(args.Entity);
            inside.Autodoc = autodoc;
            Dirty(args.Entity, inside);
        }
    }

    private void OnAutodocEntRemoved(Entity<AutodocComponent> autodoc, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != autodoc.Comp.ContainerId)
            return;

        if (autodoc.Comp.Occupant == args.Entity)
        {
            autodoc.Comp.Occupant = null;
            autodoc.Comp.IsSurgeryInProgress = false;
            autodoc.Comp.CurrentSurgeryType = AutodocSurgeryType.None;
            ResetAllTreatments(autodoc.Comp);
            Dirty(autodoc);
        }

        UpdateAutodocVisuals(autodoc);
        RemCompDeferred<InsideAutodocComponent>(args.Entity);
    }

    private void OnAutodocInteractHand(Entity<AutodocComponent> autodoc, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (autodoc.Comp.Occupant is { } occupant)
        {
            TryEjectOccupant(autodoc, occupant, args.User);
            args.Handled = true;
        }
    }

    private void OnAutodocCanDropTarget(Entity<AutodocComponent> autodoc, ref CanDropTargetEvent args)
    {
        if (!_skills.HasAllSkills(args.User, autodoc.Comp.SkillRequired))
        {
            args.CanDrop = false;
            args.Handled = true;
        }
    }

    private void OnAutodocDragDropTarget(Entity<AutodocComponent> autodoc, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!_skills.HasAllSkills(args.User, autodoc.Comp.SkillRequired))
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-no-skill"), autodoc, args.User);
            args.Handled = true;
        }
    }

    private void OnConsoleUIOpenAttempt(Entity<AutodocConsoleComponent> console, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (console.Comp.LinkedAutodoc is not { } autodocId || !TryComp<AutodocComponent>(autodocId, out var autodoc))
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-no-autodoc-connected"), console, args.User);
            args.Cancel();
        }
        else if (!_skills.HasAllSkills(args.User, autodoc.SkillRequired))
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-no-skill"), console, args.User);
            args.Cancel();
        }
    }

    private void OnConsoleInteractUsing(Entity<AutodocConsoleComponent> console, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<AutodocResearchUpgradeComponent>(args.Used, out var upgrade))
            return;

        args.Handled = true;
        if (!console.Comp.InstalledUpgrades.Add(upgrade.Tier))
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-upgrade-already-installed"), console, args.User);
            return;
        }

        Dirty(console);

        _popup.PopupEntity(Loc.GetString("rmc-autodoc-upgrade-installed"), console, args.User);

        if (_net.IsServer)
            QueueDel(args.Used);
    }

    protected void TryEjectOccupant(Entity<AutodocComponent> autodoc, EntityUid occupant, EntityUid? user = null)
    {
        if (autodoc.Comp.IsSurgeryInProgress && user == occupant)
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-cannot-exit-during-surgery"), autodoc, occupant);
            return;
        }

        if (autodoc.Comp.IsSurgeryInProgress && user != null && user != occupant)
        {
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = _random.Next(30, 50), ["Heat"] = _random.Next(30, 50) },
            };
            // TODO RMC14 Damage random limb
            _damageable.TryChangeDamage(occupant, damage, true, false);
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-surgery-aborted"), autodoc);

            _adminLog.Add(LogType.RMCAutodocSurgeryAbort, $"{ToPrettyString(user.Value):user} ejected {ToPrettyString(occupant):victim} from the autodoc during surgery, dealing {damage.GetTotal()} damage.");
        }

        EjectOccupant(autodoc, occupant);
    }

    private void EjectOccupant(Entity<AutodocComponent> autodoc, EntityUid occupant)
    {
        if (!_container.TryGetContainer(autodoc, autodoc.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);
        if (_net.IsServer)
            _audio.PlayPvs(autodoc.Comp.EjectSound, autodoc);

        if (autodoc.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(autodoc))
            _stun.TryStun(occupant, autodoc.Comp.ExitStun, true);
    }

    protected void UpdateAutodocVisuals(Entity<AutodocComponent> autodoc)
    {
        AutodocVisualState state;
        if (autodoc.Comp.Occupant == null)
            state = AutodocVisualState.Empty;
        else if (autodoc.Comp.IsSurgeryInProgress)
            state = AutodocVisualState.Operating;
        else
            state = AutodocVisualState.Occupied;

        _appearance.SetData(autodoc, AutodocVisuals.State, state);
    }

    protected static void ResetAllTreatments(AutodocComponent comp)
    {
        comp.HealingBrute = false;
        comp.HealingBurn = false;
        comp.HealingToxin = false;
        comp.BloodTransfusion = false;
        comp.Filtering = false;
        comp.CloseIncisions = false;
        comp.RemoveShrapnel = false;
        comp.InternalBleeding = false;
        comp.BrokenBone = false;
        comp.OrganDamage = false;
        comp.RemoveLarva = false;
    }

    private void OnInsideAutodocMoveInput(Entity<InsideAutodocComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Autodoc is not { } autodocId)
            return;

        if (!TryComp<AutodocComponent>(autodocId, out var autodoc))
            return;

        // Don't allow movement-based ejection during surgery
        if (autodoc.IsSurgeryInProgress)
            return;

        EjectOccupant((autodocId, autodoc), ent);
    }
}
