using System.Linq;
using Content.Shared._RMC14.Ladder;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Ladder;

public sealed class LadderSystem : SharedLadderSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private readonly HashSet<EntityUid> _toUpdate = [];
    private readonly Dictionary<string, HashSet<Entity<LadderComponent>>> _toUpdateIds = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LadderComponent, MapInitEvent>(OnLadderMapInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<LadderWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<LadderWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
    }

    public bool LadderIdInUse(string id)
    {
        var ladders = EntityQueryEnumerator<LadderComponent>();
        while (ladders.MoveNext(out _, out var ladder))
            if (ladder.Id == id)
                return true;
        return false;
    }

    public void ReassignLadderId(Entity<LadderComponent> ent, string? newId)
    {
        foreach (var connectedLadder in ent.Comp.Connected)
        {
            // Remove `ent` from `connectedLadder`.
            RemoveConnectedLadder(connectedLadder, ent);
        }
        ent.Comp.Connected.Clear();

        ent.Comp.Id = newId;
        Dirty(ent);
        _toUpdate.Add(ent);
    }

    private void OnLadderMapInit(Entity<LadderComponent> ent, ref MapInitEvent args)
    {
        _toUpdate.Add(ent);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _toUpdate.Clear();
        _toUpdateIds.Clear();
    }

    private void OnWatchingRemove<T>(Entity<LadderWatchingComponent> ent, ref T args)
    {
        RemoveWatcher(ent);
    }

    protected override void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<LadderComponent?> toWatch)
    {
        base.Watch(watcher, toWatch);

        if (!Resolve(toWatch, ref toWatch.Comp, false))
            return;

        if (watcher.Owner == toWatch.Owner)
            return;

        if (!Resolve(watcher, ref watcher.Comp1, ref watcher.Comp2) ||
            !Resolve(toWatch, ref toWatch.Comp))
        {
            return;
        }

        _eye.SetTarget(watcher, toWatch, watcher);
        _viewSubscriber.AddViewSubscriber(toWatch, watcher.Comp1.PlayerSession);

        RemoveWatcher(watcher);
        EnsureComp<LadderWatchingComponent>(watcher).Watching = toWatch;
        toWatch.Comp.Watching.Add(watcher);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        var oldTarget = watcher.Comp.Target;

        base.Unwatch(watcher, player);

        if (oldTarget != null && oldTarget != watcher.Owner)
            _viewSubscriber.RemoveViewSubscriber(oldTarget.Value, player);

        RemoveWatcher(watcher);
    }

    private void RemoveWatcher(EntityUid toRemove)
    {
        if (!TryComp(toRemove, out LadderWatchingComponent? watching))
            return;

        if (TryComp(watching.Watching, out LadderComponent? watched))
            watched.Watching.Remove(toRemove);

        watching.Watching = null;
        RemCompDeferred<LadderWatchingComponent>(toRemove);
    }

    protected override void AddViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
        base.AddViewer(ent, player);
        _viewSubscriber.AddViewSubscriber(ent, player);
    }

    protected override void RemoveViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
        base.RemoveViewer(ent, player);
        _viewSubscriber.RemoveViewSubscriber(ent, player);
    }

    public override void Update(float frameTime)
    {
        if (_toUpdate.Count == 0)
            return;

        _toUpdateIds.Clear();
        foreach (var entity in _toUpdate)
        {
            if (!LadderQuery.TryComp(entity, out var ladderComp))
                continue;

            if (ladderComp.Id is not { } id)
                continue;

            _toUpdateIds.GetOrNew(id).Add((entity, ladderComp));
        }
        _toUpdate.Clear();

        var ladders = EntityQueryEnumerator<LadderComponent>();
        while (ladders.MoveNext(out var uid, out var ladder))
        {
            if (ladder.Id == null)
                continue;

            if (!_toUpdateIds.TryGetValue(ladder.Id, out var ids))
                continue;

            // Debug-only check to make sure that each direction appears a max of once in each ID group.
            // (this means that there's currently a maximum of 3 ladders per group)
            // todo: just move this over to the mapping command as a console error thingy
            DebugTools.Assert(!ids
                .GroupBy(i => i.Comp.Direction)
                .Any(i => i.Count() > 1));

            var connectedLadders = ids
                .Where(l => l.Owner != uid)
                .Select(l => l.Owner);

            if (!connectedLadders.Any())
                continue;

            ladder.Connected = connectedLadders.ToHashSet();
            Dirty(uid, ladder);
        }
    }
}
