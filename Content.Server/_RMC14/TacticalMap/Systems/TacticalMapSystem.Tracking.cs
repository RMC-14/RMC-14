using Content.Shared._RMC14.IdentityManagement;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared.Atmos.Rotting;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.TacticalMap;

public sealed partial class TacticalMapSystem
{
    private HashSet<EntityUid> _dirtyTracked = new();
    private HashSet<EntityUid> _flushingTracked = new();
    private readonly HashSet<EntityUid> _channelBuffer = new();
    private readonly List<TacticalMapReveal> _revealBuffer = new();
    private readonly Dictionary<EntityUid, Dictionary<NetEntity, TacticalMapBlip>> _revealDesired = new();
    private readonly List<NetEntity> _revealRemoveBuffer = new();

    private void InitializeTracking()
    {
        SubscribeLocalEvent<TacticalMapTrackedComponent, MapInitEvent>(OnTrackedMapInit);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MobStateChangedEvent>(OnTrackedMobStateChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MindAddedMessage>(OnTrackedMembershipChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, HiveChangedEvent>(OnTrackedMembershipChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, SquadMemberUpdatedEvent>(OnTrackedAppearanceChanged);
        SubscribeLocalEvent<TacticalMapTrackedComponent, HiveLeaderStatusChangedEvent>(OnTrackedAppearanceChanged);

        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, MoveEvent>(OnActiveMove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, EntityRenamedEvent>(OnActiveRenamed);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, ComponentRemove>(OnActiveRemoved);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, EntityTerminatingEvent>(OnActiveRemoved);

        SubscribeLocalEvent<MapBlipIconOverrideComponent, MapInitEvent>(OnAppearanceSourceChanged);
        SubscribeLocalEvent<RottingComponent, MapInitEvent>(OnAppearanceSourceChanged);
        SubscribeLocalEvent<RottingComponent, ComponentRemove>(OnAppearanceSourceChanged);
        SubscribeLocalEvent<UnrevivableComponent, MapInitEvent>(OnAppearanceSourceChanged);
        SubscribeLocalEvent<UnrevivableComponent, ComponentRemove>(OnAppearanceSourceChanged);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnTrackedMapInit(Entity<TacticalMapTrackedComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.TrackDead && _mobState.IsDead(ent))
            return;

        MarkTrackedDirty(EnsureComp<ActiveTacticalMapTrackedComponent>(ent), ent, true, true);
    }

    private void OnTrackedMobStateChanged(Entity<TacticalMapTrackedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead && !ent.Comp.TrackDead)
        {
            RemCompDeferred<ActiveTacticalMapTrackedComponent>(ent);
            return;
        }

        if (LifeStage(ent) < EntityLifeStage.MapInitialized)
            return;

        MarkTrackedDirty(EnsureComp<ActiveTacticalMapTrackedComponent>(ent), ent, false, true);
    }

    private void OnTrackedMembershipChanged<T>(Entity<TacticalMapTrackedComponent> ent, ref T args)
    {
        if (TryComp(ent, out ActiveTacticalMapTrackedComponent? active))
            MarkTrackedDirty(active, ent, true, true);
    }

    private void OnTrackedAppearanceChanged<T>(Entity<TacticalMapTrackedComponent> ent, ref T args)
    {
        if (TryComp(ent, out ActiveTacticalMapTrackedComponent? active))
            MarkTrackedDirty(active, ent, false, true);
    }

    private void OnActiveMove(Entity<ActiveTacticalMapTrackedComponent> ent, ref MoveEvent args)
    {
        _dirtyTracked.Add(ent);
    }

    private void OnActiveRenamed(Entity<ActiveTacticalMapTrackedComponent> ent, ref EntityRenamedEvent args)
    {
        MarkTrackedDirty(ent.Comp, ent, false, true);
    }

    private void OnActiveRemoved<T>(Entity<ActiveTacticalMapTrackedComponent> ent, ref T args)
    {
        RemoveFromChannels(ent);
        _dirtyTracked.Remove(ent);
    }

    private void OnAppearanceSourceChanged<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent
    {
        if (TryComp(ent, out ActiveTacticalMapTrackedComponent? active))
            MarkTrackedDirty(active, ent, false, true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (TryComp(ev.Mob, out ActiveTacticalMapTrackedComponent? active))
            MarkTrackedDirty(active, ev.Mob, true, true);

        MarkViewerDirty(ev.Mob);
    }

    private void MarkTrackedDirty(ActiveTacticalMapTrackedComponent active, EntityUid uid, bool membership, bool appearance)
    {
        active.MembershipDirty |= membership;
        active.AppearanceDirty |= appearance;
        _dirtyTracked.Add(uid);
    }

    private void FlushTracked()
    {
        // swap so anything marked dirty during the flush waits for the next one
        (_flushingTracked, _dirtyTracked) = (_dirtyTracked, _flushingTracked);
        foreach (var uid in _flushingTracked)
        {
            if (TryComp(uid, out ActiveTacticalMapTrackedComponent? active))
                UpdateTracked((uid, active));
        }

        _flushingTracked.Clear();
        UpdateReveals();
    }

    private void UpdateTracked(Entity<ActiveTacticalMapTrackedComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var comp = ent.Comp;
        if (!TryGetTrackedPosition(ent, out var map, out var indices))
        {
            RemoveFromChannels(ent);
            comp.Map = null;
            UpdateSelf(ent, null, default, null);
            return;
        }

        if (comp.Map != map.Owner)
        {
            RemoveFromChannels(ent);
            comp.Map = map.Owner;
        }

        comp.Indices = indices;

        if (comp.AppearanceDirty)
        {
            comp.AppearanceDirty = false;
            UpdateAppearance(ent);
        }

        var tracked = CreateBlip(comp, indices);
        UpdateSelf(ent, map.Owner, indices, tracked);

        if (tracked is not { } blip)
        {
            RemoveFromChannels(ent);
            return;
        }

        if (comp.MembershipDirty)
        {
            comp.MembershipDirty = false;
            UpdateMembership(ent, map);
        }

        var net = GetNetEntity(ent);
        var alwaysVisible = HasComp<TacticalMapAlwaysVisibleComponent>(ent);
        foreach (var channel in comp.Channels)
        {
            WriteBlip(channel, net, blip, alwaysVisible);
        }
    }

    private static TacticalMapBlip? CreateBlip(ActiveTacticalMapTrackedComponent comp, Vector2i indices)
    {
        if (comp.Icon is not { } icon)
            return null;

        return new TacticalMapBlip(indices,
            icon,
            comp.Background,
            comp.Color,
            comp.Flags,
            comp.UnrevivableStart,
            comp.UnrevivableEnd,
            comp.Opacity,
            comp.Name,
            comp.Role);
    }

    private bool TryGetTrackedPosition(EntityUid uid, out Entity<TacticalMapComponent> map, out Vector2i indices)
    {
        map = default;
        indices = default;

        var target = uid;
        var xform = Transform(uid);

        // not on a tacmap grid (inside vehicle), let something else supply the position
        if (xform.GridUid is not { } ownGrid || !HasComp<TacticalMapComponent>(ownGrid))
        {
            var ev = new TacticalMapGetTrackedPositionEvent();
            RaiseLocalEvent(uid, ref ev);
            if (ev.Target is { } redirected && redirected != uid)
            {
                target = redirected;
                xform = Transform(redirected);
            }
        }

        if (xform.GridUid is not { } gridId ||
            !TryComp(gridId, out TacticalMapComponent? mapComp) ||
            !TryComp(gridId, out MapGridComponent? grid) ||
            !_transform.TryGetGridTilePosition((target, xform), out indices, grid))
        {
            return false;
        }

        map = (gridId, mapComp);
        return true;
    }

    private void UpdateAppearance(Entity<ActiveTacticalMapTrackedComponent> ent)
    {
        SpriteSpecifier.Rsi? icon = null;
        SpriteSpecifier.Rsi? background = null;
        var opacity = 1f;
        if (TryComp(ent, out TacticalMapIconComponent? iconComp))
        {
            icon = iconComp.Icon;
            background = iconComp.Background;
            opacity = iconComp.Opacity;
        }

        if (TryComp(ent, out MapBlipIconOverrideComponent? iconOverride) && iconOverride.Icon != null)
            icon = iconOverride.Icon;

        // a squad background replaces the colour tint
        var color = Color.White;
        if (_squad.TryGetMemberSquad(ent.Owner, out var squad))
        {
            if (squad.Comp.MinimapBackground != null)
                background = squad.Comp.MinimapBackground;
            else
                color = squad.Comp.Color;
        }

        var flags = TacticalMapBlipFlags.None;
        if (HasComp<HiveLeaderComponent>(ent))
            flags |= TacticalMapBlipFlags.HiveLeader;

        if (HasComp<XenoRemoteStructureRemovalComponent>(ent))
            flags |= TacticalMapBlipFlags.RemotelyRemovable;

        var start = TimeSpan.Zero;
        var end = TimeSpan.Zero;
        if (_mobState.IsDead(ent))
        {
            flags |= TacticalMapBlipFlags.Dead;
            if (HasComp<RottingComponent>(ent) || HasComp<UnrevivableComponent>(ent))
                flags |= TacticalMapBlipFlags.Undefibbable;

            if (TryComp(ent, out RMCRevivableComponent? revivable) && revivable.UnrevivableAt > TimeSpan.Zero)
            {
                end = revivable.UnrevivableAt;
                start = end - revivable.UnrevivableDelay;
            }
        }

        var comp = ent.Comp;
        comp.Icon = icon;
        comp.Background = background;
        comp.Color = color;
        comp.Opacity = opacity;
        comp.Flags = flags;
        comp.UnrevivableStart = start;
        comp.UnrevivableEnd = end;
        comp.Name = Name(ent);
        comp.Role = HasComp<XenoComponent>(ent) ? null : GetRoleName(ent);
    }

    private string? GetRevealedName(EntityUid uid)
    {
        if (!Exists(uid))
            return null;

        if (TryComp(uid, out FixedIdentityComponent? fixedIdentity) && fixedIdentity.Name is { } nameId)
        {
            var ev = new RMCGetFixedIdentityEvent(Loc.GetString(nameId));
            RaiseLocalEvent(uid, ref ev);
            return ev.Name;
        }

        if (HasComp<XenoComponent>(uid))
            return Loc.GetString("cm-xeno-name");

        return Identity.Name(uid, EntityManager).Name;
    }

    private string? GetRoleName(EntityUid uid)
    {
        var title = _squad.GetSquadTitle(uid);
        if (title.Length > 0)
            return title;

        if (TryComp(uid, out OriginalRoleComponent? original) &&
            _prototypes.TryIndex(original.Job, out var job))
        {
            return job.LocalizedName;
        }

        if (_mind.TryGetMind(uid, out var mindId, out _) &&
            _jobs.MindTryGetJobName(mindId, out var name))
        {
            return name;
        }

        return null;
    }

    private void UpdateMembership(Entity<ActiveTacticalMapTrackedComponent> ent, Entity<TacticalMapComponent> map)
    {
        _channelBuffer.Clear();

        // factions on the tracked component take priority over npc factions
        IEnumerable<ProtoId<NpcFactionPrototype>> factions;
        if (TryComp(ent, out TacticalMapTrackedComponent? tracked) && tracked.Factions.Count > 0)
            factions = tracked.Factions;
        else if (TryComp(ent, out NpcFactionMemberComponent? npc))
            factions = npc.Factions;
        else
            factions = Array.Empty<ProtoId<NpcFactionPrototype>>();

        var hive = CompOrNull<HiveMemberComponent>(ent)?.Hive;
        foreach (var faction in factions)
        {
            foreach (var (id, proto) in _channelPrototypes)
            {
                if (proto.Faction != faction)
                    continue;

                switch (proto.Scope)
                {
                    case TacticalMapChannelScope.Faction:
                        _channelBuffer.Add(GetOrCreateChannel(map, id, null));
                        break;
                    case TacticalMapChannelScope.Hive when hive != null:
                        _channelBuffer.Add(GetOrCreateChannel(map, id, hive));
                        break;
                }
            }
        }

        var net = GetNetEntity(ent);
        foreach (var old in ent.Comp.Channels)
        {
            if (!_channelBuffer.Contains(old))
                RemoveBlip(old, net);
        }

        ent.Comp.Channels.Clear();
        ent.Comp.Channels.UnionWith(_channelBuffer);
    }

    private void WriteBlip(EntityUid channel, NetEntity net, TacticalMapBlip blip, bool alwaysVisible)
    {
        if (!TryComp(channel, out TacticalMapChannelComponent? comp) ||
            comp.LiveFeed is not { } feedUid ||
            !TryComp(feedUid, out TacticalMapLiveFeedComponent? feed))
        {
            return;
        }

        // a real blip takes over a revealed one
        feed.Revealed.Remove(net);
        if (!feed.Blips.TryGetValue(net, out var existing) || existing != blip)
        {
            feed.Blips[net] = blip;
            feed.Modified[net] = _timing.CurTick;
            Dirty(feedUid, feed);
        }

        // always visible blips are kept in the snapshot
        if (!alwaysVisible || !TryComp(channel, out TacticalMapSnapshotComponent? snapshot))
            return;

        snapshot.AlwaysVisible.Add(net);
        if (!snapshot.Blips.TryGetValue(net, out var snapped) || snapped != blip)
        {
            snapshot.Blips[net] = blip;
            Dirty(channel, snapshot);
        }
    }

    private void RemoveBlip(EntityUid channel, NetEntity net)
    {
        if (!TryComp(channel, out TacticalMapChannelComponent? comp))
            return;

        if (comp.LiveFeed is { } feedUid &&
            TryComp(feedUid, out TacticalMapLiveFeedComponent? feed) &&
            feed.Blips.Remove(net))
        {
            feed.Modified.Remove(net);
            feed.Revealed.Remove(net);
            Dirty(feedUid, feed);
        }

        if (TryComp(channel, out TacticalMapSnapshotComponent? snapshot) && snapshot.AlwaysVisible.Remove(net))
        {
            snapshot.Blips.Remove(net);
            Dirty(channel, snapshot);
        }
    }

    private void RemoveFromChannels(Entity<ActiveTacticalMapTrackedComponent> ent)
    {
        if (ent.Comp.Channels.Count > 0)
        {
            var net = GetNetEntity(ent);
            foreach (var channel in ent.Comp.Channels)
            {
                RemoveBlip(channel, net);
            }

            ent.Comp.Channels.Clear();
        }

        ent.Comp.MembershipDirty = true;
    }

    // copies another factions blips into a channels feed
    private void UpdateReveals()
    {
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var mapUid, out var mapComp))
        {
            _revealBuffer.Clear();
            var ev = new TacticalMapCollectRevealsEvent(mapUid, _revealBuffer);
            RaiseLocalEvent(ref ev);

            var sides = GetSideChannels(mapComp);
            _revealDesired.Clear();

            foreach (var reveal in _revealBuffer)
            {
                var rangeSquared = reveal.Range * reveal.Range;
                foreach (var receiver in sides)
                {
                    if (receiver.Comp.Faction != reveal.To)
                        continue;

                    if (!_revealDesired.TryGetValue(receiver.FeedUid, out var desired))
                    {
                        desired = new Dictionary<NetEntity, TacticalMapBlip>();
                        _revealDesired[receiver.FeedUid] = desired;
                    }

                    foreach (var source in sides)
                    {
                        if (source.Channel == receiver.Channel || source.Comp.Faction != reveal.From)
                            continue;

                        foreach (var (net, blip) in source.Feed.Blips)
                        {
                            // no chaining reveals
                            if (source.Feed.Revealed.Contains(net))
                                continue;

                            // the receiver already tracks it for real
                            if (receiver.Feed.Blips.ContainsKey(net) && !receiver.Feed.Revealed.Contains(net))
                                continue;

                            if (reveal.Center is { } center)
                            {
                                var dx = blip.Indices.X - center.X;
                                var dy = blip.Indices.Y - center.Y;
                                if (dx * dx + dy * dy > rangeSquared)
                                    continue;
                            }

                            desired[net] = blip with
                            {
                                Flags = blip.Flags | TacticalMapBlipFlags.Revealed,
                                Name = GetRevealedName(GetEntity(net)),
                                Role = null,
                            };
                        }
                    }
                }
            }

            foreach (var side in sides)
            {
                ApplyReveals(side, _revealDesired.GetValueOrDefault(side.FeedUid));
            }
        }
    }

    private void ApplyReveals(SideChannel side, Dictionary<NetEntity, TacticalMapBlip>? desired)
    {
        var feed = side.Feed;
        var dirty = false;

        _revealRemoveBuffer.Clear();
        foreach (var net in feed.Revealed)
        {
            if (desired == null || !desired.ContainsKey(net))
                _revealRemoveBuffer.Add(net);
        }

        foreach (var net in _revealRemoveBuffer)
        {
            feed.Revealed.Remove(net);
            feed.Blips.Remove(net);
            feed.Modified.Remove(net);
            dirty = true;
        }

        if (desired != null)
        {
            foreach (var (net, blip) in desired)
            {
                if (feed.Blips.TryGetValue(net, out var existing) && existing == blip)
                    continue;

                feed.Blips[net] = blip;
                feed.Modified[net] = _timing.CurTick;
                feed.Revealed.Add(net);
                dirty = true;
            }
        }

        if (dirty)
            Dirty(side.FeedUid, feed);
    }

    // own position goes on the user component so it shows even when not on any channel
    private void UpdateSelf(EntityUid uid, EntityUid? map, Vector2i indices, TacticalMapBlip? blip)
    {
        if (!_selfTracked.Contains(uid) || !TryComp(uid, out TacticalMapUserComponent? user))
            return;

        if (user.SelfMap == map && user.SelfIndices == indices && user.SelfBlip == blip)
            return;

        user.SelfMap = map;
        user.SelfIndices = indices;
        user.SelfBlip = blip;
        Dirty(uid, user);
    }
}
