using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.TacticalMap;

public sealed partial class TacticalMapSystem
{
    private readonly record struct SideChannel(
        EntityUid Channel,
        TacticalMapChannelComponent Comp,
        EntityUid FeedUid,
        TacticalMapLiveFeedComponent Feed);

    private readonly List<(EntProtoId Id, TacticalMapChannelComponent Comp)> _channelPrototypes = new();
    private readonly List<TacticalMapChannelKey> _channelKeyBuffer = new();
    private readonly List<SideChannel> _sideChannelBuffer = new();

    private void InitializeChannels()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<TacticalMapComponent, MapInitEvent>(OnTacticalMapMapInit);
        SubscribeLocalEvent<TacticalMapComponent, ComponentShutdown>(OnTacticalMapShutdown);
        SubscribeLocalEvent<SquadObjectivesChangedEvent>(OnSquadObjectivesChanged);

        RebuildChannelPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            RebuildChannelPrototypes();
    }

    private void RebuildChannelPrototypes()
    {
        _channelPrototypes.Clear();
        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || !proto.TryGetComponent(out TacticalMapChannelComponent? channel, _compFactory))
                continue;

            _channelPrototypes.Add((proto.ID, channel));
        }

        _channelPrototypes.Sort((a, b) => a.Comp.SortOrder.CompareTo(b.Comp.SortOrder));
    }

    private void OnTacticalMapMapInit(Entity<TacticalMapComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.DisplayName))
            ent.Comp.DisplayName = Name(ent);

        ent.Comp.BaseDirty = true;
        foreach (var (id, proto) in _channelPrototypes)
        {
            if (proto.Scope == TacticalMapChannelScope.Faction)
                GetOrCreateChannel(ent, id, null);
        }

        var tracked = EntityQueryEnumerator<ActiveTacticalMapTrackedComponent>();
        while (tracked.MoveNext(out var uid, out var active))
        {
            active.MembershipDirty = true;
            _dirtyTracked.Add(uid);
        }

        Dirty(ent);
        MarkAllSessionsDirty();
    }

    private void OnTacticalMapShutdown(Entity<TacticalMapComponent> ent, ref ComponentShutdown args)
    {
        foreach (var channel in ent.Comp.Channels.Values)
        {
            DeleteChannel(channel);
        }

        ent.Comp.Channels.Clear();
        MarkAllSessionsDirty();
    }

    private void OnSquadObjectivesChanged(ref SquadObjectivesChangedEvent args)
    {
        var squad = args.Squad;
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var map))
        {
            foreach (var (key, channel) in map.Channels)
            {
                if (key.Owner != squad.Owner || !TryComp(channel, out TacticalMapObjectivesComponent? objectives))
                    continue;

                objectives.Objectives = new Dictionary<SquadObjectiveType, string>(squad.Comp.Objectives);
                Dirty(channel, objectives);
            }
        }
    }

    private EntityUid GetOrCreateChannel(Entity<TacticalMapComponent> map, EntProtoId prototype, EntityUid? owner)
    {
        var key = new TacticalMapChannelKey(prototype.Id, owner);
        if (map.Comp.Channels.TryGetValue(key, out var existing) && !TerminatingOrDeleted(existing))
            return existing;

        var uid = Spawn(prototype, MapCoordinates.Nullspace);
        var channel = EnsureComp<TacticalMapChannelComponent>(uid);
        channel.Map = map;
        channel.OwnerId = owner == null ? null : GetNetEntity(owner.Value);
        EnsureComp<TacticalMapCanvasComponent>(uid);

        if (channel.Scope == TacticalMapChannelScope.Squad)
        {
            var objectives = EnsureComp<TacticalMapObjectivesComponent>(uid);
            if (owner != null && TryComp(owner, out SquadTeamComponent? squad))
            {
                channel.Color = squad.Color;
                objectives.Objectives = new Dictionary<SquadObjectiveType, string>(squad.Objectives);
                _metaData.SetEntityName(uid, Name(owner.Value));
            }

            Dirty(uid, objectives);
        }
        else
        {
            EnsureComp<TacticalMapSnapshotComponent>(uid);

            var feed = Spawn(LiveFeedPrototype, MapCoordinates.Nullspace);
            EnsureComp<TacticalMapLiveFeedComponent>(feed).Channel = uid;
            channel.LiveFeed = feed;

            if (owner != null && HasComp<HiveComponent>(owner))
                _metaData.SetEntityName(uid, Name(owner.Value));
        }

        Dirty(uid, channel);
        map.Comp.Channels[key] = uid;
        MarkAllSessionsDirty();
        return uid;
    }

    private void DeleteChannel(EntityUid channel)
    {
        if (TryComp(channel, out TacticalMapChannelComponent? comp) && comp.LiveFeed is { } feed)
            QueueDel(feed);

        QueueDel(channel);
    }

    private void CleanupOrphanChannels()
    {
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var map))
        {
            _channelKeyBuffer.Clear();
            foreach (var (key, channel) in map.Channels)
            {
                if (TerminatingOrDeleted(channel) || key.Owner is { } owner && TerminatingOrDeleted(owner))
                    _channelKeyBuffer.Add(key);
            }

            foreach (var key in _channelKeyBuffer)
            {
                if (map.Channels.Remove(key, out var channel))
                    DeleteChannel(channel);
            }

            if (_channelKeyBuffer.Count > 0)
                MarkAllSessionsDirty();
        }
    }

    private List<SideChannel> GetSideChannels(TacticalMapComponent map)
    {
        _sideChannelBuffer.Clear();
        foreach (var channel in map.Channels.Values)
        {
            if (!TryComp(channel, out TacticalMapChannelComponent? comp) ||
                comp.LiveFeed is not { } feedUid ||
                !TryComp(feedUid, out TacticalMapLiveFeedComponent? feed))
            {
                continue;
            }

            _sideChannelBuffer.Add(new SideChannel(channel, comp, feedUid, feed));
        }

        return _sideChannelBuffer;
    }
}
