using Content.Shared._RMC14.Xenonids;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Dropship;

public abstract class SharedDropshipSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SharedDropshipNavigationComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SharedDropshipNavigationComputerComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<SharedDropshipNavigationComputerComponent, AfterActivatableUIOpenEvent>(OnNavigationOpen);

        SubscribeLocalEvent<DropshipTerminalComponent, ActivateInWorldEvent>(OnDropshipTerminalActivateInWorld);

        Subs.BuiEvents<SharedDropshipNavigationComputerComponent>(DropshipNavigationUiKey.Key,
            subs =>
            {
                subs.Event<DropshipNavigationLaunchMsg>(OnDropshipNavigationLaunchMsg);
            });

        Subs.BuiEvents<SharedDropshipNavigationComputerComponent>(DropshipHijackerUiKey.Key,
            subs =>
            {
                subs.Event<DropshipHijackerDestinationChosenBuiMsg>(OnHijackerDestinationChosenMsg);
            });
    }

    private void OnMapInit(Entity<SharedDropshipNavigationComputerComponent> ent, ref MapInitEvent args)
    {
        var transformComp = Transform(ent);
        if (transformComp.ParentUid is { Valid: true } parent &&
            IsShuttle(parent))
        {
            EnsureComp<DropshipComponent>(parent);
        }

        var dropshipGrid = transformComp.GridUid;
        var lockableDoorQuery = EntityQueryEnumerator<LockableDropshipDoorComponent>();

        while (lockableDoorQuery.MoveNext(out EntityUid doorEnt, out LockableDropshipDoorComponent? doorComp))
        {
            if (doorComp is null)
            {
                continue;
            }

            if (Transform(doorEnt).GridUid == dropshipGrid)
            {
                if (ent.Comp.LockableDoors.TryGetValue(doorComp.LocName, out List<EntityUid>? doorEntList))
                {
                    doorEntList.Add(doorEnt);
                }
                else
                {
                    var newDoorEntList = new List<EntityUid>();
                    newDoorEntList.Add(doorEnt);
                    ent.Comp.LockableDoors.Add(doorComp.LocName, newDoorEntList);
                }
            }
        }
    }

    private void OnUIOpenAttempt(Entity<SharedDropshipNavigationComputerComponent> ent,
        ref ActivatableUIOpenAttemptEvent args)
    {
        if (TryComp(ent, out TransformComponent? xform) &&
            TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            args.Cancel();
        }
    }

    private void OnNavigationOpen(Entity<SharedDropshipNavigationComputerComponent> ent, ref AfterActivatableUIOpenEvent args)
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

        var userTransform = Transform(user);

        Entity<TransformComponent>? closestDestination = null;
        var destinations = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
        while (destinations.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != userTransform.MapID)
                continue;

            if (closestDestination == null)
            {
                closestDestination = (uid, xform);
                continue;
            }

            if (userTransform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance) &&
                userTransform.Coordinates.TryDistance(EntityManager,
                    closestDestination.Value.Comp.Coordinates,
                    out var oldDistance) &&
                distance < oldDistance)
            {
                closestDestination = (uid, xform);
            }
        }

        if (closestDestination == null)
        {
            _popup.PopupEntity("There are no dropship destinations near you!", user, user, PopupType.MediumCaution);
            return;
        }

        var dropships = EntityQueryEnumerator<DropshipComponent>();
        while (dropships.MoveNext(out var uid, out var dropship))
        {
            if (dropship.Crashed || IsInFTL(uid))
                continue;

            var computerQuery = EntityQueryEnumerator<SharedDropshipNavigationComputerComponent>();
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

    private void OnDropshipNavigationLaunchMsg(Entity<SharedDropshipNavigationComputerComponent> ent,
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

    private void OnHijackerDestinationChosenMsg(Entity<SharedDropshipNavigationComputerComponent> ent,
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

    public virtual bool FlyTo(Entity<SharedDropshipNavigationComputerComponent> computer,
        EntityUid destination,
        EntityUid? user,
        bool hijack = false)
    {
        return false;
    }

    protected virtual void RefreshUI(Entity<SharedDropshipNavigationComputerComponent> computer)
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
}
