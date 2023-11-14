using Content.Shared.Movement.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._CM14.Xenos.Watch;

public abstract class SharedWatchXenoSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoWatchActionEvent>(OnXenoWatch);
        SubscribeLocalEvent<XenoComponent, XenoWatchBuiMessage>(OnXenoWatchBui);
        SubscribeLocalEvent<XenoComponent, MoveInputEvent>(OnXenoMoveInput);
    }

    private void OnXenoMoveInput(Entity<XenoComponent> ent, ref MoveInputEvent args)
    {
        if (_net.IsClient && _player.LocalEntity == ent.Owner && _player.LocalSession != null)
            Unwatch(ent.Owner, _player.LocalSession);
        else if (TryComp(ent, out ActorComponent? actor))
            Unwatch(ent.Owner, actor.PlayerSession);
    }

    private void OnXenoWatchBui(Entity<XenoComponent> ent, ref XenoWatchBuiMessage args)
    {
        if (!TryGetEntity(args.Target, out var target))
            return;

        Watch(ent.Owner, target.Value);
    }


    protected virtual void OnXenoWatch(Entity<XenoComponent> ent, ref XenoWatchActionEvent args)
    {
    }

    protected virtual void Watch(Entity<XenoComponent?, ActorComponent?, EyeComponent?> watcher, Entity<XenoComponent?> watch)
    {
    }

    protected virtual void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        _eye.SetTarget(watcher, watcher, watcher);
    }
}
