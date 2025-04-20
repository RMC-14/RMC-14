using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Movement.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Watch;

public abstract class SharedXenoWatchSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoWatchActionEvent>(OnXenoWatchAction);
        SubscribeLocalEvent<XenoWatchingComponent, MoveInputEvent>(OnXenoMoveInput);

        Subs.BuiEvents<XenoComponent>(XenoWatchUIKey.Key,
            subs =>
            {
                subs.Event<XenoWatchBuiMsg>(OnXenoWatchBui);
            });
    }

    private void OnXenoMoveInput(Entity<XenoWatchingComponent> xeno, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_net.IsClient && _player.LocalEntity == xeno.Owner && _player.LocalSession != null)
            Unwatch(xeno.Owner, _player.LocalSession);
        else if (TryComp(xeno, out ActorComponent? actor))
            Unwatch(xeno.Owner, actor.PlayerSession);
        else if (TryComp(xeno, out QueenEyeComponent? eye) && TryComp(eye.Queen, out ActorComponent? eyeActor))
            Unwatch(xeno.Owner, eyeActor.PlayerSession);
    }

    private void OnXenoWatchBui(Entity<XenoComponent> xeno, ref XenoWatchBuiMsg args)
    {
        if (!TryGetEntity(args.Target, out var target))
            return;

        Watch(xeno.Owner, target.Value);
    }

    protected virtual void OnXenoWatchAction(Entity<XenoComponent> ent, ref XenoWatchActionEvent args)
    {
    }

    public virtual void Watch(Entity<HiveMemberComponent?, ActorComponent?, EyeComponent?> watcher, Entity<HiveMemberComponent?> toWatch)
    {
    }

    protected virtual void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        _eye.SetTarget(watcher, null, watcher);
        var ev = new XenoUnwatchEvent();
        RaiseLocalEvent(watcher, ref ev);
    }

    public bool TryGetWatched(Entity<XenoWatchingComponent?> watching, out EntityUid watched)
    {
        if (!Resolve(watching, ref watching.Comp, false) ||
            watching.Comp.Watching == null)
        {
            watched = default;
            return false;
        }

        watched = watching.Comp.Watching.Value;
        return true;
    }

    public void SetWatching(Entity<XenoWatchingComponent?> watching, EntityUid target)
    {
        watching.Comp = EnsureComp<XenoWatchingComponent>(watching);
        watching.Comp.Watching = target;
        Dirty(watching);
    }
}
