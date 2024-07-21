using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Dropship;

public abstract class SharedDropshipSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private TimeSpan _dropshipInitialDelay;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipNavigationComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, AfterActivatableUIOpenEvent>(OnNavigationOpen);

        SubscribeLocalEvent<DropshipTerminalComponent, ActivateInWorldEvent>(OnDropshipTerminalActivateInWorld);

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

    private void OnDropshipNavigationLaunchMsg(Entity<DropshipNavigationComputerComponent> ent,
        ref DropshipNavigationLaunchMsg args)
    {
        _ui.CloseUi(ent.Owner, DropshipNavigationUiKey.Key, args.Actor);

        if (!TryGetEntity(args.Target, out var destination))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to launch to invalid dropship destination {args.Target}");
            return;
        }

        if (!TryComp(destination, out DropshipDestinationComponent? destinationComp))
        {
            Log.Warning(
                $"{ToPrettyString(args.Actor)} tried to launch to invalid dropship destination {ToPrettyString(destination)}");
            return;
        }

        if (destinationComp.Ship != null)
        {
            Log.Warning(
                $"{ToPrettyString(args.Actor)} tried to launch to occupied dropship destination {ToPrettyString(destination)}");
            return;
        }

        FlyTo(ent, destination.Value, args.Actor);
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
        }
    }

    public virtual bool FlyTo(Entity<DropshipNavigationComputerComponent> computer,
        EntityUid destination,
        EntityUid? user,
        bool hijack = false)
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
            var minutesLeft = Math.Max(1, (int) (_dropshipInitialDelay- roundDuration).TotalMinutes);
            var msg = Loc.GetString("rmc-dropship-pre-flight-fueling", ("minutes", minutesLeft));

            if (predicted)
                _popup.PopupClient(msg, computer, user, PopupType.MediumCaution);
            else
                _popup.PopupEntity(msg, computer, user, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    public bool TryDesignatePrimaryLZ(EntityUid actor, EntityUid lz, SoundSpecifier sound)
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

        if (HasComp<AlmayerComponent>(_transform.GetGrid(lz)) ||
            HasComp<AlmayerComponent>(_transform.GetMap(lz)))
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
        _marineAnnounce.AnnounceARES(actor, $"Command Order Issued:\n\n{Name(lz)} has been designated as the primary landing zone.", sound);

        return true;
    }

    public IEnumerable<Entity<MetaDataComponent>> GetPrimaryLZCandidates()
    {
        if (Count<PrimaryLandingZoneComponent>() != 0)
            yield break;

        var landingZoneQuery = EntityQueryEnumerator<DropshipDestinationComponent, MetaDataComponent, TransformComponent>();
        while (landingZoneQuery.MoveNext(out var uid, out _, out var metaData, out var xform))
        {
            if (HasComp<AlmayerComponent>(xform.ParentUid) || HasComp<AlmayerComponent>(xform.MapUid))
                continue;

            yield return (uid, metaData);
        }
    }
}
