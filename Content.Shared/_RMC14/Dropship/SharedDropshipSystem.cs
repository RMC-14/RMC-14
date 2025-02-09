using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;

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

    private TimeSpan _dropshipInitialDelay;
    private TimeSpan _hijackInitialDelay;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipComponent, MapInitEvent>(OnDropshipMapInit);

        SubscribeLocalEvent<DropshipNavigationComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, AfterActivatableUIOpenEvent>(OnNavigationOpen);

        SubscribeLocalEvent<DropshipTerminalComponent, ActivateInWorldEvent>(OnDropshipTerminalActivateInWorld);

        SubscribeLocalEvent<DropshipWeaponPointComponent, MapInitEvent>(OnAttachmentPointMapInit);
        SubscribeLocalEvent<DropshipWeaponPointComponent, EntityTerminatingEvent>(OnAttachmentPointRemove);

        SubscribeLocalEvent<DropshipUtilityPointComponent, MapInitEvent>(OnAttachmentPointMapInit);
        SubscribeLocalEvent<DropshipUtilityPointComponent, EntityTerminatingEvent>(OnAttachmentPointRemove);
        SubscribeLocalEvent<DropshipWeaponPointComponent, ExaminedEvent>(OnAttachmentExamined);

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key,
            subs =>
            {
                subs.Event<DropshipNavigationLaunchMsg>(OnDropshipNavigationLaunchMsg);
            });

        Subs.BuiEvents<DropshipNavigationComputerComponent>(DropshipHijackerUiKey.Key,
            subs =>
            {
                subs.Event<DropshipHijackerDestinationChosenBuiMsg>(OnHijackerDestinationChosenMsg);
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

            if (HasComp<DropshipWeaponPointComponent>(uid))
                ent.Comp.AttachmentPoints.Add(uid);

            if (HasComp<DropshipUtilityPointComponent>(uid))
                ent.Comp.AttachmentPoints.Add(uid);
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

        var xform = Transform(ent);
        if (TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            args.Cancel();
            return;
        }

        if (!TryDropshipLaunchPopup(ent, args.User, true))
            args.Cancel();
    }

    private void OnNavigationOpen(Entity<DropshipNavigationComputerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        RefreshUI(ent);
    }

    private void OnDropshipTerminalActivateInWorld(Entity<DropshipTerminalComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_net.IsClient)
            return;

        var user = args.User;
        if (!HasComp<XenoComponent>(user))
        {
            _popup.PopupEntity("This terminal doesn't seem to work yet... Maybe you should ask High Command?", user, user, PopupType.MediumCaution);
            return;
        }

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

        if (closestDestination == null)
        {
            _popup.PopupEntity("There are no dropship destinations near you!", user, user, PopupType.MediumCaution);
            return;
        }
        else if (closestDestination.Value.Comp1.Ship != null)
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

        var dropships = EntityQueryEnumerator<DropshipComponent>();
        while (dropships.MoveNext(out var uid, out var dropship))
        {
            if (dropship.Crashed || IsInFTL(uid))
                continue;

            var computerQuery = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
            while (computerQuery.MoveNext(out var computerId, out var computer))
            {
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

    private void OnAttachmentPointMapInit(Entity<DropshipWeaponPointComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (TryGetGridDropship(ent, out var dropship))
        {
            dropship.Comp.AttachmentPoints.Add(ent);
            Dirty(dropship);
        }
    }

    private void OnAttachmentPointMapInit(Entity<DropshipUtilityPointComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (TryGetGridDropship(ent, out var dropship))
        {
            dropship.Comp.AttachmentPoints.Add(ent);
            Dirty(dropship);
        }
    }

    private void OnAttachmentPointRemove<T>(Entity<DropshipWeaponPointComponent> ent, ref T args)
    {
        if (TryGetGridDropship(ent, out var dropship))
        {
            dropship.Comp.AttachmentPoints.Remove(ent);
            Dirty(dropship);
        }
    }

    private void OnAttachmentPointRemove<T>(Entity<DropshipUtilityPointComponent> ent, ref T args)
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
            if (TryGetPointContained(ent, ent.Comp.WeaponContainerSlotId, out var weapon))
                args.PushText(Loc.GetString("rmc-dropship-weapons-point-gun", ("weapon", weapon)));

            if (TryGetPointContained(ent, ent.Comp.AmmoContainerSlotId, out var ammo))
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

    private void OnDropshipNavigationLaunchMsg(Entity<DropshipNavigationComputerComponent> ent,
        ref DropshipNavigationLaunchMsg args)
    {
        var user = args.Actor;
        _ui.CloseUi(ent.Owner, DropshipNavigationUiKey.Key, user);

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
        float? hyperspaceTime = null)
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
        RefreshUI();

        var message = Loc.GetString("rmc-announcement-ares-lz-designated", ("name", Name(lz)));
        _marineAnnounce.AnnounceARES(actor, message);

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

    private bool TryGetPointContained(
        Entity<DropshipWeaponPointComponent> point,
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
}
