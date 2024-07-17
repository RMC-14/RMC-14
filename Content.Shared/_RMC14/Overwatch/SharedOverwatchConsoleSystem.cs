using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Overwatch;

public abstract class SharedOverwatchConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private EntityQuery<MarineComponent> _marine;
    private EntityQuery<SquadMemberComponent> _squadMember;

    public override void Initialize()
    {
        _marine = GetEntityQuery<MarineComponent>();
        _squadMember = GetEntityQuery<SquadMemberComponent>();

        SubscribeLocalEvent<OverwatchConsoleComponent, BoundUIOpenedEvent>(OnBUIOpened);

        SubscribeLocalEvent<OverwatchWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);

        Subs.BuiEvents<OverwatchConsoleComponent>(OverwatchConsoleUI.Key, subs =>
        {
            subs.Event<OverwatchConsoleWatchBuiMsg>(OnOverwatchWatchBui);
        });
    }

    private void OnBUIOpened(Entity<OverwatchConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (_net.IsClient)
            return;

        var marines = new List<OverwatchMarine>();
        var query = EntityQueryEnumerator<OverwatchCameraComponent, TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var xform, out var metaData))
        {
            if (!_container.TryGetContainingContainer((uid, xform, metaData), out var container))
                continue;

            var name = "Unknown";
            if (_marine.HasComp(container.Owner))
                name = Identity.Name(container.Owner, EntityManager);

            var squadName = "Unknown";
            if (_squadMember.TryComp(container.Owner, out var member) && member.Squad != null)
                squadName = Name(member.Squad.Value);

            marines.Add(new OverwatchMarine(GetNetEntity(uid), squadName, name));
        }

        var state = new OverwatchConsoleBuiState(marines);
        _ui.SetUiState(ent.Owner, OverwatchConsoleUI.Key, state);
    }

    private void OnWatchingMoveInput(Entity<OverwatchWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (_net.IsClient && _player.LocalEntity == ent.Owner && _player.LocalSession != null)
            Unwatch(ent.Owner, _player.LocalSession);
        else if (TryComp(ent, out ActorComponent? actor))
            Unwatch(ent.Owner, actor.PlayerSession);
    }

    private void OnOverwatchWatchBui(Entity<OverwatchConsoleComponent> ent, ref OverwatchConsoleWatchBuiMsg args)
    {
        if (!TryGetEntity(args.Target, out var target))
            return;

        Watch(args.Actor, target.Value);
    }

    protected virtual void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<OverwatchCameraComponent?> toWatch)
    {
    }

    protected virtual void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        _eye.SetTarget(watcher, watcher, watcher);
    }
}
