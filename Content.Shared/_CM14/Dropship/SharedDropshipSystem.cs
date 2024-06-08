using Content.Shared._CM14.Marines.Announce;
using Content.Shared.GameTicking;
using Content.Shared.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Dropship;

public abstract class SharedDropshipSystem : EntitySystem
{
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipNavigationComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<DropshipNavigationComputerComponent, AfterActivatableUIOpenEvent>(OnNavigationOpen);

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
    }

    private void OnMapInit(Entity<DropshipNavigationComputerComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).ParentUid is { Valid: true } parent &&
            IsShuttle(parent))
        {
            EnsureComp<DropshipComponent>(parent);
        }
    }

    private void OnUIOpenAttempt(Entity<DropshipNavigationComputerComponent> ent,
        ref ActivatableUIOpenAttemptEvent args)
    {
        if (TryComp(ent, out TransformComponent? xform) &&
            TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            args.Cancel();
        }
    }

    private void OnNavigationOpen(Entity<DropshipNavigationComputerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        RefreshUI(ent);
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

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var roundTime = time - _gameTicker.RoundStartTimeSpan;
        var computers = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computer))
        {
            if (Transform(computerId).GridUid is not { } dropshipId ||
                !TryComp(dropshipId, out DropshipComponent? dropship))
            {
                continue;
            }

            if (roundTime < dropship.AutoRecallRoundDelay)
                continue;

            var dropshipName = Name(dropshipId);
            if (string.IsNullOrWhiteSpace(dropshipName))
                dropshipName = "Dropship";

            if (TryComp(dropship.Destination, out DropshipDestinationComponent? currentDestination) &&
                currentDestination.AutoRecall)
            {
                if (dropship.AutoRecallAt != default)
                {
                    dropship.AutoRecallAt = default;
                    Dirty(dropshipId, dropship);
                }

                continue;
            }

            if (dropship.AutoRecallAt == default)
            {
                dropship.AutoRecallAt = time + dropship.AutoRecallTime;
                Dirty(dropshipId, dropship);

                var minutes = (int) dropship.AutoRecallTime.TotalMinutes;
                var seconds = dropship.AutoRecallTime.Seconds;
                var timeString = string.Empty;
                if (minutes > 0)
                    timeString += $" {minutes} minutes";

                if (seconds > 0)
                    timeString += $" {seconds} seconds";

                _marineAnnounce.Announce(dropshipId, $"The {dropshipName} will be automatically recalled to the planet in{timeString}.", dropship.AnnounceAutoRecallIn);
            }

            if (time < dropship.AutoRecallAt)
                continue;

            if (IsInFTL(dropshipId))
                continue;

            var destinations = EntityQueryEnumerator<DropshipDestinationComponent>();
            while (destinations.MoveNext(out var destinationId, out var destinationComp))
            {
                if (!destinationComp.AutoRecall || destinationComp.Ship != null)
                    continue;

                if (FlyTo((computerId, computer), destinationId, computerId))
                {
                    dropship.AutoRecallAt = default;
                    _marineAnnounce.Announce(dropshipId, $"The {dropshipName} has been automatically called to {Name(destinationId)}.", dropship.AnnounceAutoRecallIn);
                    break;
                }
            }
        }
    }
}
