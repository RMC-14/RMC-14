using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Watch;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._CM14.Xenos.Watch;

public sealed class XenoWatchSystem : SharedWatchXenoSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    protected override void OnXenoWatch(Entity<XenoComponent> ent, ref XenoWatchEvent args)
    {
        if (!TryComp(ent, out ActorComponent? actor))
            return;

        _ui.TryOpen(ent.Owner, XenoWatchUIKey.Key, actor.PlayerSession);

        var xenos = new List<Xeno>();

        if (ent.Comp.Hive != default)
        {
            var query = EntityQueryEnumerator<XenoComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out var xeno, out var metaData))
            {
                if (uid == ent.Owner || xeno.Hive != ent.Comp.Hive)
                    continue;

                xenos.Add(new Xeno(GetNetEntity(uid), Name(uid, metaData), metaData.EntityPrototype?.ID));
            }
        }

        _ui.TrySetUiState(ent.Owner, XenoWatchUIKey.Key, new XenoWatchBuiState(xenos));
    }

    protected override void Watch(Entity<XenoComponent?, ActorComponent?, EyeComponent?> watcher, Entity<XenoComponent?> watch)
    {
        base.Watch(watcher, watch);

        if (!Resolve(watcher, ref watcher.Comp1, ref watcher.Comp2, ref watcher.Comp3) ||
            !Resolve(watch, ref watch.Comp))
        {
            return;
        }

        if (watcher.Comp1.Hive != watch.Comp.Hive || watch.Comp.Hive == default)
            return;

        _eye.SetTarget(watcher, watch, watcher);
        _viewSubscriber.AddViewSubscriber(watch, watcher.Comp2.PlayerSession);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        var oldTarget = watcher.Comp.Target;

        base.Unwatch(watcher, player);

        if (oldTarget != null && oldTarget != watcher.Owner)
            _viewSubscriber.RemoveViewSubscriber(oldTarget.Value, player);
    }
}
