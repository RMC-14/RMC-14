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
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private readonly HashSet<EntityUid> _toUpdate = [];
    private readonly Dictionary<string, HashSet<Entity<LadderComponent>>> _toUpdateIds = [];


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<LadderComponent, MapInitEvent>(OnLadderMapInit);
        SubscribeLocalEvent<LadderWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<LadderWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
    }

    public List<Entity<LadderComponent>> GetLadderGroup(string groupId)
    {
        List<Entity<LadderComponent>> ladderGroup = [];

        var ladders = EntityQueryEnumerator<LadderComponent>();
        while (ladders.MoveNext(out var ladder, out var ladderComp))
        {
            if (ladderComp.GroupId == groupId)
                ladderGroup.Add((ladder, ladderComp));
        }

        return ladderGroup;
    }

    /// <summary>
    /// Link <paramref name="ladder"/> to any other ladders with an <see cref="LadderComponent.Id"/> of <paramref name="newGroupId"/>,
    /// by updating the <see cref="LadderComponent.Above"/> and <see cref="LadderComponent.Below"/> of each.
    /// </summary>
    /// <remarks>
    /// If there aren't any <paramref name="newGroupId"/> ladders yet, <paramref name="ladder"/> will be set as the first.
    /// </remarks>
    /// <param name="ladder">Ladder entity to be added to the group.</param>
    /// <param name="newGroupId">ID string of the group <paramref name="ladder"/> should be added to.</param>
    /// <seealso cref="TryRemoveFromGroup(string, Entity{LadderComponent}, out string?)"/>
    public bool TryAddToGroup(Entity<LadderComponent?> ladder, string newGroupId)
    {
        if (!Resolve(ladder, ref ladder.Comp))
            return false;

        if (ladder.Comp.GroupId == newGroupId)
            return false;

        var group = GetLadderGroup(newGroupId);
        if (group.TryFirstOrNull(l => l.Comp.Level == ladder.Comp.Level, out var sameLevelLadder))
        {
            Log.Error($"Failed to add {ToPrettyString(ladder)} to group '{newGroupId}'. {ToPrettyString(sameLevelLadder)} has a duplicate 'Level' value of {ladder.Comp.Level}!");
            return false;
        }

        ladder.Comp.GroupId = newGroupId;
        group.Add((ladder.Owner, ladder.Comp));
        UpdateAdjacent(group);
        return true;
    }

    /// <summary>
    /// Unlink <paramref name="ladder"/> from any other ladders with an <see cref="LadderComponent.Id"/> of <paramref name="oldGroupId"/>,
    /// and update the <see cref="LadderComponent.Above"/> and <see cref="LadderComponent.Below"/> of any ladders remaining.
    /// </summary>
    /// <param name="ladder">Ladder entity to be removed from the group.</param>
    /// <param name="oldGroupId">ID string of the group <paramref name="ladder"/> should be removed from.</param>
    /// <seealso cref="TryAddToGroup(Entity{LadderComponent?}, string)"/>
    public bool TryRemoveFromGroup(Entity<LadderComponent?> ladder, string oldGroupId)
    {
        if (!Resolve(ladder, ref ladder.Comp))
            return false;

        if (ladder.Comp.GroupId != oldGroupId)
            return false;

        ladder.Comp.GroupId = null;
        Dirty(ladder);
        var group = GetLadderGroup(oldGroupId);
        group.Remove((ladder.Owner, ladder.Comp));
        UpdateAdjacent(group);
        return true;
    }

    /// <summary>
    /// Change the <see cref="LadderComponent.Level"/> of <paramref name="ladder"/> to <paramref name="newLevel"/>.
    /// </summary>
    /// <remarks>
    /// If <paramref name="ladder"/> shares a <see cref="LadderComponent.GroupId"/> with other ladders,
    /// then <paramref name="newLevel"/> must be a unique value among them.
    /// </remarks>
    public bool TrySetLevel(Entity<LadderComponent?> ladder, int newLevel)
    {
        if (!Resolve(ladder, ref ladder.Comp))
            return false;

        if (ladder.Comp.GroupId == null)
        {
            ladder.Comp.Level = newLevel;
            return true;
        }

        var group = GetLadderGroup(ladder.Comp.GroupId);
        if (group.TryFirstOrNull(l => l.Comp.Level == newLevel, out var sameLevelLadder))
        {
            Log.Error($"Failed to change the Level of {ToPrettyString(ladder)} to {newLevel}. {ToPrettyString(sameLevelLadder)} already holds that position!");
            return false;
        }

        ladder.Comp.Level = newLevel;
        UpdateAdjacent(group);
        return true;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _toUpdate.Clear();
        _toUpdateIds.Clear();
    }

    private void OnLadderMapInit(Entity<LadderComponent> ent, ref MapInitEvent args)
    {
        _toUpdate.Add(ent);
    }

    private void OnWatchingRemove<T>(Entity<LadderWatchingComponent> ent, ref T args)
    {
        RemoveWatcher(ent);
    }

    protected override void OpenRadialMenu(Entity<LadderComponent> ent, EntityUid user, SelectionReason reason)
    {
        if (ent.Comp.Above is not { } above || ent.Comp.Below is not { } below)
        {
            Log.Error($"Ladder {ToPrettyString(ent)} tried to open a radial menu, but doesn't have two connected ladders! (Above: {ToPrettyString(ent.Comp.Above)} | Below: {ToPrettyString(ent.Comp.Below)})");
            return;
        }

        _uiSystem.OpenUi(ent.Owner, LadderRadialBuiKey.Key, user);
        _uiSystem.SetUiState(ent.Owner, LadderRadialBuiKey.Key, new LadderRadialBuiState(GetNetEntity(above), GetNetEntity(below), reason));
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

    /// <summary>
    /// Given a list of ladders with the same <see cref="LadderComponent.Id"/>, loop through the list in pairs of two and link each ladder's
    /// <see cref="LadderComponent.Above"/> and <see cref="LadderComponent.Below"/> to each other, in order of their <see cref="LadderComponent.Level"/>.
    /// </summary>
    /// <param name="group">List of ladders to link. (LLL)</param>
    private void UpdateAdjacent(IEnumerable<Entity<LadderComponent>> group)
    {
        var orderedGroup = group.OrderBy(l => l.Comp.Level).ToList();
        if (orderedGroup.Count < 2)
            return;

        // Reset the top and bottom elements just in case something got deleted.
        orderedGroup[0].Comp.Below = null;
        orderedGroup[^1].Comp.Above = null;

        // `.Zip()` of the list, and the list with the first element skipped, so it returns a tuple of two elements at a time.
        foreach (var (current, next) in orderedGroup.Zip(orderedGroup.Skip(1)))
        {
            DebugTools.AssertEqual(current.Comp.GroupId, next.Comp.GroupId); // shouldn't ever get this far but you know ¯\(ツ)/¯
            current.Comp.Above = next;
            next.Comp.Below = current;
        }

        // Doing this seperately for the sake of simplicity. If it was in the loop above each ladder could be dirtied twice.
        foreach (var ladder in orderedGroup)
            Dirty(ladder);
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

            if (ladderComp.GroupId is not { } id)
                continue;

            _toUpdateIds.GetOrNew(id).Add((entity, ladderComp));
        }
        _toUpdate.Clear();

        var ladders = EntityQueryEnumerator<LadderComponent>();
        while (ladders.MoveNext(out _, out var ladder))
        {
            if (ladder.GroupId == null)
                continue;

            if (!_toUpdateIds.TryGetValue(ladder.GroupId, out var ladderGroup))
                continue;

            UpdateAdjacent(ladderGroup);
        }
    }
}
