using Content.Server.Chat.Managers;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._CM14.Dropship;
using Content.Shared._CM14.Xenos;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._CM14.Dropship;

public sealed class DropshipSystem : SharedDropshipSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropshipNavigationComputerComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<DropshipComponent, FTLRequestEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLStartedEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLCompletedEvent>(OnRefreshUI);
        SubscribeLocalEvent<DropshipComponent, FTLUpdatedEvent>(OnRefreshUI);
    }

    private void OnActivateInWorld(Entity<DropshipNavigationComputerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!HasComp<DropshipHijackerComponent>(args.User))
            return;

        if (TryComp(ent, out TransformComponent? xform) &&
            TryComp(xform.ParentUid, out DropshipComponent? dropship) &&
            dropship.Crashed)
        {
            return;
        }

        args.Handled = true;

        var destinations = new List<(NetEntity Id, string Name)>();
        var query = EntityQueryEnumerator<DropshipHijackDestinationComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            destinations.Add((GetNetEntity(uid), Name(uid)));
        }

        _ui.OpenUi(ent.Owner, DropshipHijackerUiKey.Key, args.User);
        _ui.SetUiState(ent.Owner, DropshipHijackerUiKey.Key, new DropshipHijackerBuiState(destinations));
    }

    private void OnRefreshUI<T>(Entity<DropshipComponent> ent, ref T args)
    {
        RefreshUI();
    }

    protected override bool FlyTo(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination, EntityUid user, bool hijack = false)
    {
        base.FlyTo(computer, destination, user, hijack);

        var shuttle = Transform(computer).GridUid;
        if (!TryComp(shuttle, out ShuttleComponent? shuttleComp))
        {
            Log.Warning($"Tried to launch through dropship computer {ToPrettyString(computer)} outside of a shuttle.");
            return false;
        }

        if (HasComp<FTLComponent>(shuttle))
        {
            Log.Warning($"Tried to launch shuttle {ToPrettyString(shuttle)} in FTL");
            return false;
        }

        var dropship = EnsureComp<DropshipComponent>(shuttle.Value);
        if (dropship.Crashed)
        {
            Log.Warning($"Tried to launch crashed dropship {ToPrettyString(shuttle.Value)}");
            return false;
        }

        if (dropship.Destination == destination)
        {
            Log.Warning($"Tried to launch {ToPrettyString(shuttle.Value)} to its current destination {ToPrettyString(destination)}.");
            return false;
        }

        dropship.Destination = destination;
        Dirty(shuttle.Value, dropship);

        var destTransform = Transform(destination);
        var destCoords = _transform.GetMoverCoordinates(destination, destTransform);
        var rotation = destTransform.LocalRotation;
        if (TryComp(shuttle, out PhysicsComponent? physics))
            destCoords = destCoords.Offset(-physics.LocalCenter);

        _shuttle.FTLToCoordinates(shuttle.Value, shuttleComp, destCoords, rotation);

        if (hijack && CompOrNull<XenoComponent>(user)?.Hive is { } hive)
        {
            var xenos = Filter
                .Empty()
                .AddWhereAttachedEntity(ent => CompOrNull<XenoComponent>(ent)?.Hive == hive);
            var text = "The Queen has commanded the metal bird to depart for the metal hive in the sky! Rejoice!";
            var wrapped = $"[color=#921992][font size=16][bold]{text}[/bold][/font][/color]";

            _chat.ChatMessageToManyFiltered(xenos, ChatChannel.Radio, text, wrapped, user, false, true, null);
        }

        return true;
    }

    protected override void RefreshUI(Entity<DropshipNavigationComputerComponent> computer)
    {
        if (!_ui.IsUiOpen(computer.Owner, DropshipNavigationUiKey.Key))
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

            _ui.SetUiState(computer.Owner, DropshipNavigationUiKey.Key, new DropshipNavigationDestinationsBuiState(destinations));
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
        _ui.SetUiState(computer.Owner, DropshipNavigationUiKey.Key, state);
    }

    private void RefreshUI()
    {
        var computers = EntityQueryEnumerator<DropshipNavigationComputerComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            RefreshUI((uid, comp));
        }
    }

    public void RaiseUpdate(EntityUid shuttle)
    {
        var ev = new FTLUpdatedEvent();
        RaiseLocalEvent(shuttle, ref ev);
    }
}
