using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.Tracker;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Maturing;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Dropship;

public abstract class SharedDropshipSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private TimeSpan _dropshipInitialDelay;
    private TimeSpan _hijackInitialDelay;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipComponent, MapInitEvent>(OnDropshipMapInit);

        SubscribeLocalEvent<DropshipNavigationComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, AfterActivatableUIOpenEvent>(OnNavigationOpen);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, DropshipLockoutOverrideDoAfterEvent>(OnNavigationLockoutOverride);

        SubscribeLocalEvent<DropshipTerminalComponent, ActivateInWorldEvent>(OnDropshipTerminalActivateInWorld, before: [typeof(ActivatableUISystem), typeof(ActivatableUIRequiresAccessSystem)]);
        SubscribeLocalEvent<DropshipTerminalComponent, ActivatableUIOpenAttemptEvent>(OnTerminalOpenAttempt);
        SubscribeLocalEvent<DropshipTerminalComponent, AfterActivatableUIOpenEvent>(OnTerminalOpen);

        SubscribeLocalEvent<DropshipWeaponPointComponent, MapInitEvent>(OnAttachmentPointMapInit);
        SubscribeLocalEvent<DropshipWeaponPointComponent, EntityTerminatingEvent>(OnAttachmentPointRemove);
        SubscribeLocalEvent<DropshipWeaponPointComponent, ExaminedEvent>(OnAttachmentExamined);

        SubscribeLocalEvent<DropshipUtilityPointComponent, MapInitEvent>(OnAttachmentPointMapInit);
        SubscribeLocalEvent<DropshipUtilityPointComponent, EntityTerminatingEvent>(OnAttachmentPointRemove);

        SubscribeLocalEvent<DropshipEnginePointComponent, MapInitEvent>(OnAttachmentPointMapInit);
        SubscribeLocalEvent<DropshipEnginePointComponent, EntityTerminatingEvent>(OnAttachmentPointRemove);
        SubscribeLocalEvent<DropshipEnginePointComponent, ExaminedEvent>(OnEngineExamined);

        SubscribeLocalEvent<DropshipElectronicSystemPointComponent, MapInitEvent>(OnAttachmentPointMapInit);
        SubscribeLocalEvent<DropshipElectronicSystemPointComponent, EntityTerminatingEvent>(OnAttachmentPointRemove);
        SubscribeLocalEvent<DropshipElectronicSystemPointComponent, ExaminedEvent>(OnElectronicSystemExamined);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key,
            subs =>
            {
                subs.Event<DropshipNavigationLaunchMsg>(OnDropshipNavigationLaunchMsg);
                subs.Event<DropshipNavigationCancelMsg>(OnDropshipNavigationCancelMsg);
            });

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipHijackerUiKey.Key,
            subs =>
            {
                subs.Event<DropshipHijackerDestinationChosenBuiMsg>(OnHijackerDestinationChosenMsg);
            });

        Subs.BuiEvents<DropshipTerminalComponent>(DropshipTerminalUiKey.Key,
            subs =>
            {
                subs.Event<DropshipTerminalSummonDropshipMsg>(OnTerminalSummon);
            });

        Subs.CVar(_config, RMCCVars.RMCDropshipInitialDelayMinutes, v => _dropshipInitialDelay = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCDropshipHijackInitialDelayMinutes, v => _hijackInitialDelay = TimeSpan.FromMinutes(v), true);
    }

    private void OnDropshipMapInit(Entity<DropshipComponent> ent, ref MapInitEvent args)
    {
        var children = Transform(ent).ChildEnumerator;
        while (children.MoveNext(out var uid))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (HasComp<DropshipWeaponPointComponent>(uid) ||
                HasComp<DropshipEnginePointComponent>(uid) ||
                HasComp<DropshipUtilityPointComponent>(uid) ||
                HasComp<DropshipElectronicSystemPointComponent>(uid))
            {
                ent.Comp.AttachmentPoints.Add(uid);
            }
        }

        var ev = new DropshipMapInitEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnMapInit(Entity<DropshipNavigationComputerComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).ParentUid is { Valid: true } parent &&
            IsShuttle(parent))
        {
            EnsureComp<DropshipComponent>(parent);
        }
    }

    private void OnUIOpenAttempt(Entity<DropshipNavigationComputerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<XenoComponent>(args.User) && !HasComp<DropshipHijackerComponent>(args.User))
        {
            args.Cancel();
            return;
        }

        var xform = Transform(ent);
        if (TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            args.Cancel();
            return;
        }

        if (!TryDropshipLaunchPopup(ent, args.User, true))
        {
            args.Cancel();
            return;
        }

        var lockedOutRemaining = ent.Comp.LockedOutUntil - _timing.CurTime;
        if (lockedOutRemaining > TimeSpan.Zero && !HasComp<DropshipHijackerComponent>(args.User))
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString("rmc-dropship-locked-out", ("minutes", (int)lockedOutRemaining.TotalMinutes)), ent, args.User, PopupType.MediumCaution);

            if (_skills.HasSkill(args.User, ent.Comp.Skill, ent.Comp.FlyBySkillLevel))
            {
                var ev = new DropshipLockoutOverrideDoAfterEvent();
                var doAfter = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(20), ev, ent, ent)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    BreakOnRest = true,
                    DuplicateCondition = DuplicateConditions.SameEvent,
                    CancelDuplicate = true
                };
                _doAfter.TryStartDoAfter(doAfter);
            }
            return;
        }

        if (lockedOutRemaining <= TimeSpan.Zero && HasComp<DropshipHijackerComponent>(args.User))
        {
            args.Cancel();

            var ev = new DropshipLockoutDoAfterEvent();
            var doAfter = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(3), ev, ent, ent)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnRest = true,
                DuplicateCondition = DuplicateConditions.SameEvent,
                CancelDuplicate = true
            };
            _doAfter.TryStartDoAfter(doAfter);
            return;
        }

        // Queen only from here on.
        if (!HasComp<DropshipHijackerComponent>(args.User))
            return;

        args.Cancel();

        if (!TryDropshipHijackPopup(ent, args.User, false))
            return;

        var destinations = new List<(NetEntity Id, string Name)>();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            destinations.Add((GetNetEntity(uid), Name(uid)));
        }

        _ui.OpenUi(ent.Owner, DropshipHijackerUiKey.Key, args.User);
        _ui.SetUiState(ent.Owner, DropshipHijackerUiKey.Key, new DropshipHijackerBuiState(destinations));
    }

    private void OnNavigationOpen(Entity<DropshipNavigationComputerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        RefreshUI(ent);
    }

    private void OnNavigationLockoutOverride(Entity<DropshipNavigationComputerComponent> ent, ref DropshipLockoutOverrideDoAfterEvent args)
    {
        var lockedOutRemaining = ent.Comp.LockedOutUntil - _timing.CurTime;
        var reduction = lockedOutRemaining / 10 + TimeSpan.FromSeconds(20);
        ent.Comp.LockedOutUntil -= reduction;
        Dirty(ent);

        if (ent.Comp.LockedOutUntil < _timing.CurTime)
        {
            _ui.CloseUis(ent.Owner);
            _popup.PopupClient(Loc.GetString("rmc-dropship-locked-out-bypass-complete"), ent, args.User, PopupType.Medium);
            return;
        }

        _popup.PopupClient(Loc.GetString("rmc-dropship-locked-out-bypass"), ent, args.User, PopupType.Medium);
    }

    private void OnDropshipTerminalActivateInWorld(Entity<DropshipTerminalComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        if (!HasComp<XenoComponent>(user))
        {
            // not handled -> Open the UI for marines.
            return;
        }

        args.Handled = true;
        if (_net.IsClient)
            return;

        if (!HasComp<DropshipHijackerComponent>(user))
        {
            _popup.PopupEntity($"You stare cluelessly at the {Name(ent.Owner)}", user, user);
            return;
        }

        if (!TryDropshipLaunchPopup(ent, user, false))
            return;

        if (!TryDropshipHijackPopup(ent, user, false))
            return;

        var userTransform = Transform(user);
        var closestDestination = FindClosestLZ(userTransform);
        if (closestDestination == null)
        {
            _popup.PopupEntity("There are no dropship destinations near you!", user, user, PopupType.MediumCaution);
            return;
        }

        if (closestDestination.Value.Comp1.Ship != null)
        {
            _popup.PopupEntity("There's already a dropship coming here!", user, user, PopupType.MediumCaution);
            return;
        }

        if (Count<PrimaryLandingZoneComponent>() > 0 &&
            !HasComp<PrimaryLandingZoneComponent>(closestDestination))
        {
            _popup.PopupEntity("The shuttle isn't responding to prompts, it looks like this isn't the primary shuttle.", user, user, PopupType.MediumCaution);
            return;
        }

        var dropships = EntityQueryEnumerator<DropshipComponent, TransformComponent>();
        while (dropships.MoveNext(out var uid, out var dropship, out var xform))
        {
            if (dropship.Crashed || IsInFTL(uid))
                continue;

            if (HasComp<ThunderdomeMapComponent>(xform.MapUid))
                continue;

            var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
            while (computerQuery.MoveNext(out var computerId, out var computer))
            {
                if (!computer.Hijackable)
                    continue;

                if (Transform(computerId).GridUid == uid &&
                    FlyTo((computerId, computer), closestDestination.Value, user))
                {
                    _popup.PopupEntity("You call down one of the dropships to your location", user, user, PopupType.LargeCaution);
                    return;
                }
            }
        }

        _popup.PopupEntity("There are no available dropships! Wait a moment.", user, user, PopupType.LargeCaution);
    }

    private void OnTerminalOpenAttempt(Entity<DropshipTerminalComponent> terminal, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<XenoComponent>(args.User))
            args.Cancel();
    }

    private void OnTerminalOpen(Entity<DropshipTerminalComponent> terminal, ref AfterActivatableUIOpenEvent args)
    {
        if (!_ui.IsUiOpen(terminal.Owner, DropshipTerminalUiKey.Key, args.Actor))
            return;

        var closestLZ = FindClosestLZ(terminal);
        if (closestLZ is not { } lz)
        {
            var failedState = new DropshipTerminalBuiState("???", []);
            _ui.SetUiState(terminal.Owner, DropshipTerminalUiKey.Key, failedState);
            return;
        }

        var dropships = new List<DropshipEntry>();
        var dropshipQuery = EntityQueryEnumerator<DropshipComponent>();
        while (dropshipQuery.MoveNext(out var uid, out var _))
        {
            var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
            while (computerQuery.MoveNext(out var computerId, out var computer))
            {
                // ERT-Ships can't be hijacked, so we can use this to filter them out.
                if (!computer.Hijackable)
                    continue;

                // On a different grid => not the associated computer.
                if (Transform(computerId).GridUid != uid)
                    continue;

                dropships.Add(new DropshipEntry(GetNetEntity(computerId), Name(uid)));
            }
        }

        var state = new DropshipTerminalBuiState(Name(lz), dropships);
        _ui.SetUiState(terminal.Owner, DropshipTerminalUiKey.Key, state);
    }

    private void OnTerminalSummon(Entity<DropshipTerminalComponent> terminal, ref DropshipTerminalSummonDropshipMsg args)
    {
        if (_net.IsClient)
            return;

        if (!_ui.IsUiOpen(terminal.Owner, DropshipTerminalUiKey.Key, args.Actor))
            return;

        if (!TryGetEntity(args.Id, out var computerId) ||
            !TryComp<DropshipNavigationComputerComponent>(computerId, out var computer) ||
            !computer.Hijackable)
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to remotely pilot a invalid dropship");
            return;
        }

        var closestDestination = FindClosestLZ(terminal);
        if (closestDestination == null)
        {
            _popup.PopupEntity("There are no dropship destinations near you!", terminal, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (closestDestination.Value.Comp1.Ship is { } ship)
        {
            if (HasComp<FTLComponent>(ship))
            {
                _popup.PopupEntity("There is already a dropship coming here!", terminal, args.Actor, PopupType.MediumCaution);
            }
            else
            {
                _popup.PopupEntity("There is already a dropship here!", terminal, args.Actor, PopupType.MediumCaution);
            }
            return;
        }

        if (!computer.RemoteControl)
        {
            _popup.PopupEntity("This dropship does not have remote-control enabled.", terminal, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!TryDropshipLaunchPopup(terminal, args.Actor, false))
            return;

        if (!FlyTo((computerId.Value, computer), closestDestination.Value, args.Actor))
        {
            _popup.PopupEntity("This dropship is currently busy. Please try again later.", terminal, args.Actor, PopupType.MediumCaution);
            return;
        }

        _ui.CloseUi(terminal.Owner, DropshipTerminalUiKey.Key, args.Actor);
        _popup.PopupEntity("This dropship is now on its way.", terminal, args.Actor, PopupType.Medium);
    }

    private void OnAttachmentPointMapInit<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent?
    {
        if (_net.IsClient)
            return;

        if (TryGetGridDropship(ent, out var dropship))
        {
            dropship.Comp.AttachmentPoints.Add(ent);
            Dirty(dropship);
        }
    }

    private void OnAttachmentPointRemove<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent?
    {
        if (TryGetGridDropship(ent, out var dropship))
        {
            dropship.Comp.AttachmentPoints.Remove(ent);
            Dirty(dropship);
        }
    }

    private void OnAttachmentExamined(Entity<DropshipWeaponPointComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(DropshipWeaponPointComponent)))
        {
            if (TryGetAttachmentContained(ent, ent.Comp.WeaponContainerSlotId, out var weapon))
                args.PushText(Loc.GetString("rmc-dropship-attached", ("attachment", weapon)));

            if (TryGetAttachmentContained(ent, ent.Comp.AmmoContainerSlotId, out var ammo))
            {
                args.PushText(Loc.GetString("rmc-dropship-weapons-point-ammo", ("ammo", ammo)));

                if (TryComp(ammo, out DropshipAmmoComponent? ammoComp))
                {
                    args.PushText(Loc.GetString("rmc-dropship-weapons-rounds-left",
                        ("current", ammoComp.Rounds),
                        ("max", (ammoComp.MaxRounds))));
                }
            }
        }
    }

    private void OnEngineExamined(Entity<DropshipEnginePointComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(DropshipWeaponPointComponent)))
        {
            if (TryGetAttachmentContained(ent, ent.Comp.ContainerId, out var attachment))
                args.PushText(Loc.GetString("rmc-dropship-attached", ("attachment", attachment)));
        }
    }

    private void OnElectronicSystemExamined(Entity<DropshipElectronicSystemPointComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(DropshipWeaponPointComponent)))
        {
            if (TryGetAttachmentContained(ent, ent.Comp.ContainerId, out var attachment))
                args.PushText(Loc.GetString("rmc-dropship-attached", ("attachment", attachment)));
        }
    }

    private void OnDropshipNavigationLaunchMsg(Entity<DropshipNavigationComputerComponent> ent,
        ref DropshipNavigationLaunchMsg args)
    {
        var user = args.Actor;

        if (!TryGetEntity(args.Target, out var destination))
        {
            Log.Warning($"{ToPrettyString(user)} tried to launch to invalid dropship destination {args.Target}");
            return;
        }

        if (!HasComp<DropshipDestinationComponent>(destination))
        {
            Log.Warning(
                $"{ToPrettyString(args.Actor)} tried to launch to invalid dropship destination {ToPrettyString(destination)}");
            return;
        }

        FlyTo(ent, destination.Value, user);
    }

    private void OnDropshipNavigationCancelMsg(Entity<DropshipNavigationComputerComponent> ent,
        ref DropshipNavigationCancelMsg args)
    {
        var grid = _transform.GetGrid((ent.Owner, Transform(ent.Owner)));
        if (!TryComp(grid, out FTLComponent? ftl) || !TryComp(grid, out DropshipComponent? dropship))
            return;

        if (dropship.Destination != dropship.DepartureLocation ||
            _timing.CurTime + dropship.CancelFlightTime >= ftl.StateTime.End)
            return;

        ftl.StateTime.End = _timing.CurTime + dropship.CancelFlightTime;
        Dirty(grid.Value, dropship);
        RefreshUI();
    }

    private void OnHijackerDestinationChosenMsg(Entity<DropshipNavigationComputerComponent> ent,
        ref DropshipHijackerDestinationChosenBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, DropshipHijackerUiKey.Key, args.Actor);

        if (!TryGetEntity(args.Destination, out var destination))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to hijack to invalid destination");
            return;
        }

        if (!HasComp<DropshipHijackDestinationComponent>(destination))
        {
            Log.Warning(
                $"{ToPrettyString(args.Actor)} tried to hijack to invalid destination {ToPrettyString(destination)}");
            return;
        }

        if (FlyTo(ent, destination.Value, args.Actor, true) &&
            TryComp(ent, out TransformComponent? xform) &&
            xform.ParentUid.Valid)
        {
            var dropship = EnsureComp<DropshipComponent>(xform.ParentUid);
            dropship.Crashed = true;
            Dirty(xform.ParentUid, dropship);

            var ev = new DropshipHijackStartEvent(xform.ParentUid);
            RaiseLocalEvent(ref ev);
        }
    }

    public virtual bool FlyTo(Entity<DropshipNavigationComputerComponent> computer,
        EntityUid destination,
        EntityUid? user,
        bool hijack = false,
        float? startupTime = null,
        float? hyperspaceTime = null,
        bool offset = false)
    {
        return false;
    }

    protected virtual void RefreshUI()
    {
    }

    protected virtual void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
    }

    protected virtual bool IsShuttle(EntityUid dropship)
    {
        return false;
    }

    protected virtual bool IsInFTL(EntityUid dropship)
    {
        return false;
    }

    private bool TryDropshipLaunchPopup(EntityUid computer, EntityUid user, bool predicted)
    {
        var roundDuration = _gameTicker.RoundDuration();
        if (roundDuration < _dropshipInitialDelay)
        {
            var minutesLeft = Math.Max(1, (int)(_dropshipInitialDelay - roundDuration).TotalMinutes);
            var msg = Loc.GetString("rmc-dropship-pre-flight-fueling", ("minutes", minutesLeft));

            if (predicted)
                _popup.PopupClient(msg, computer, user, PopupType.MediumCaution);
            else
                _popup.PopupEntity(msg, computer, user, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    protected bool TryDropshipHijackPopup(EntityUid computer, Entity<DropshipHijackerComponent?> user, bool predicted)
    {
        var roundDuration = _gameTicker.RoundDuration();
        if (HasComp<DropshipHijackerComponent>(user) && roundDuration < _hijackInitialDelay)
        {
            var minutesLeft = Math.Max(1, (int)(_hijackInitialDelay - roundDuration).TotalMinutes);
            var msg = Loc.GetString("rmc-dropship-pre-hijack", ("minutes", minutesLeft));

            if (predicted)
                _popup.PopupClient(msg, computer, user, PopupType.MediumCaution);
            else
                _popup.PopupEntity(msg, computer, user, PopupType.MediumCaution);

            return false;
        }

        var map = _transform.GetMap(user.Owner);

        // Prevent shipside hijacks by immature queens.
        if (HasComp<XenoMaturingComponent>(user) &&
            !HasComp<RMCPlanetComponent>(map) ||
            // Prevent double hijack.
            TryComp(map, out EvacuationProgressComponent? evacuation) &&
            evacuation.DropShipCrashed)
        {
            var msg = Loc.GetString("rmc-dropship-invalid-hijack");

            if (predicted)
                _popup.PopupClient(msg, computer, user, PopupType.MediumCaution);
            else
                _popup.PopupEntity(msg, computer, user, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    public bool TryDesignatePrimaryLZ(
        EntityUid actor,
        EntityUid lz)
    {
        if (!HasComp<DropshipDestinationComponent>(lz))
        {
            Log.Warning($"{ToPrettyString(actor)} tried to designate as primary LZ entity {ToPrettyString(lz)} with no {nameof(DropshipDestinationComponent)}!");
            return false;
        }

        if (Count<PrimaryLandingZoneComponent>() > 0)
        {
            Log.Warning($"{ToPrettyString(actor)} tried to designate as primary LZ entity {ToPrettyString(lz)} when one already exists!");
            return false;
        }

        if (!HasComp<RMCPlanetComponent>(_transform.GetGrid(lz)) &&
            !HasComp<RMCPlanetComponent>(_transform.GetMap(lz)))
        {
            Log.Warning($"{ToPrettyString(actor)} tried to designate entity {ToPrettyString(lz)} on the warship as primary LZ!");
            return false;
        }

        if (GetPrimaryLZCandidates().All(candidate => candidate.Owner != lz))
        {
            Log.Warning($"{ToPrettyString(actor)} tried to designate invalid primary LZ entity {ToPrettyString(lz)}!");
            return false;
        }

        _adminLog.Add(LogType.RMCPrimaryLZ, $"{ToPrettyString(actor):player} designated {ToPrettyString(lz):lz} as primary landing zone");

        EnsureComp<PrimaryLandingZoneComponent>(lz);
        EnsureComp<RMCTrackableComponent>(lz);
        RefreshUI();

        var message = Loc.GetString("rmc-announcement-ares-lz-designated", ("name", Name(lz)));
        _marineAnnounce.AnnounceARESStaging(actor, message);

        return true;
    }

    public IEnumerable<Entity<MetaDataComponent>> GetPrimaryLZCandidates()
    {
        if (Count<PrimaryLandingZoneComponent>() != 0)
            yield break;

        var landingZoneQuery = EntityQueryEnumerator<DropshipDestinationComponent, MetaDataComponent, TransformComponent>();
        while (landingZoneQuery.MoveNext(out var uid, out _, out var metaData, out var xform))
        {
            if (!HasComp<RMCPlanetComponent>(xform.ParentUid) &&
                !HasComp<RMCPlanetComponent>(xform.MapUid))
            {
                continue;
            }

            yield return (uid, metaData);
        }
    }

    public bool TryGetGridDropship(EntityUid ent, out Entity<DropshipComponent> dropship)
    {
        if (TryComp(ent, out TransformComponent? xform) &&
            xform.GridUid is { } grid &&
            !TerminatingOrDeleted(grid) &&
            TryComp(xform.GridUid, out DropshipComponent? dropshipComp))
        {
            dropship = (grid, dropshipComp);
            return true;
        }

        dropship = default;
        return false;
    }

    public bool IsWeaponAttached(Entity<DropshipWeaponComponent?> weapon)
    {
        if (!Resolve(weapon, ref weapon.Comp, false) ||
            !TryGetGridDropship(weapon, out var dropship))
        {
            return false;
        }

        if (!_container.TryGetContainingContainer((weapon, null), out var container) ||
            !dropship.Comp.AttachmentPoints.Contains(container.Owner))
        {
            return false;
        }

        return true;
    }
    // wtf why was it private
    public bool TryGetAttachmentContained(
        EntityUid point,
        string containerId,
        out EntityUid contained)
    {
        contained = default;
        if (!_container.TryGetContainer(point, containerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return false;
        }

        contained = container.ContainedEntities[0];
        return true;
    }

    public bool IsInFlight(Entity<DropshipComponent?> dropship)
    {
        if (!Resolve(dropship, ref dropship.Comp, false))
            return false;

        return dropship.Comp.State == FTLState.Travelling || dropship.Comp.State == FTLState.Arriving;
    }

    public bool IsOnDropship(EntityUid entity)
    {
        var grid = _transform.GetGrid(entity);
        return HasComp<DropshipComponent>(grid);
    }

    public bool IsOnDropship(EntityCoordinates coordinates)
    {
        var grid = _transform.GetGrid(coordinates);
        return HasComp<DropshipComponent>(grid);
    }

    public Entity<DropshipDestinationComponent, TransformComponent>? FindClosestLZ(TransformComponent userTransform)
    {
        Entity<DropshipDestinationComponent, TransformComponent>? closestDestination = null;
        var destinations = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
        while (destinations.MoveNext(out var uid, out var destination, out var xform))
        {
            if (xform.MapID != userTransform.MapID)
                continue;

            if (closestDestination == null)
            {
                closestDestination = (uid, destination, xform);
                continue;
            }

            if (userTransform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance) &&
                userTransform.Coordinates.TryDistance(EntityManager,
                    closestDestination.Value.Comp2.Coordinates,
                    out var oldDistance) &&
                distance < oldDistance)
            {
                closestDestination = (uid, destination, xform);
            }
        }
        return closestDestination;
    }

    public Entity<DropshipDestinationComponent, TransformComponent>? FindClosestLZ(EntityUid entity)
    {
        if (TryComp(entity, out TransformComponent? transform))
            return FindClosestLZ(transform);

        return null;
    }
}
