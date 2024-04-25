using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._CM14.Dropship;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._CM14.Dropship;

public sealed class DropshipSystem : SharedDropshipSystem
{
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropshipComponent, FTLRequestEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLStartedEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLCompletedEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLUpdatedEvent>(OnRefreshUI);
    }

    private void OnRefreshUI<T>(Entity<DropshipComponent> ent, ref T args)
    {
        RefreshUI();
    }

    protected override void Interact(Entity<DropshipNavigationComputerComponent> ent, EntityUid user)
    {
        base.Interact(ent, user);

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _ui.TryOpen(ent, DropshipNavigationUiKey.Key, actor.PlayerSession);
        RefreshUI(ent);
    }

    protected override void FlyTo(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination)
    {
        base.FlyTo(computer, destination);

        var shuttle = Transform(computer).GridUid;
        if (!TryComp(shuttle, out ShuttleComponent? shuttleComp))
        {
            Log.Warning($"Tried to launch through dropship computer {ToPrettyString(computer)} outside of a shuttle.");
            return;
        }

        if (HasComp<FTLComponent>(shuttle))
        {
            Log.Warning($"Tried to launch shuttle {ToPrettyString(shuttle)} in FTL");
            return;
        }

        var destTransform = Transform(destination);
        var destCoords = _transform.GetMoverCoordinates(destination, destTransform);
        var rotation = destTransform.LocalRotation;
        if (TryComp(shuttle, out PhysicsComponent? physics))
            destCoords = destCoords.Offset(-physics.LocalCenter);

        EnsureComp<DropshipComponent>(shuttle.Value).Destination = destination;
        _shuttle.FTLToCoordinates(shuttle.Value, shuttleComp, destCoords, rotation);
    }

    private void RefreshUI()
    {
        var computers = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            RefreshUI((uid, comp));
        }
    }

    private void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
        if (!_ui.IsUiOpen(computer, DropshipNavigationUiKey.Key))
            return;

        if (Transform(computer).GridUid is not { } grid)
            return;

        if (!TryComp(grid, out FTLComponent? ftl) ||
            !ftl.Running ||
            ftl.State == FTLState.Available)
        {
            var destinations = new List<(NetEntity Id, string Name)>();
            var query = EntityQueryEnumerator<DropshipDestinationComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                destinations.Add((GetNetEntity(uid), Name(uid)));
            }

            _ui.TrySetUiState(computer, DropshipNavigationUiKey.Key, new DropshipNavigationDestinationsBuiState(destinations));
            return;
        }

        var destination = string.Empty;
        if (TryComp(grid, out DropshipComponent? dropship) &&
            dropship.Destination is { } destinationUid)
        {
            destination = Name(destinationUid);
        }
        else
        {
            Log.Error($"Found in-travel dropship {ToPrettyString(grid)} with invalid destination");
        }

        var state = new DropshipNavigationTravellingBuiState(ftl.State, ftl.StateTime, destination);
        _ui.TrySetUiState(computer, DropshipNavigationUiKey.Key, state);
    }

    public void RaiseUpdate(EntityUid shuttle)
    {
        var ev = new FTLUpdatedEvent();
        RaiseLocalEvent(shuttle, ref ev);
    }
}
