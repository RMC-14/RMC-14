using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Inventory.Events;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._RMC14.TacticalMap;

public sealed partial class TacticalMapSystem
{
    private const float MinStrokeThickness = 1f;
    private const float MaxStrokeThickness = 8f;
    private const int BoundsMargin = 2;

    private sealed class ViewerSession
    {
        public readonly Dictionary<(EntityUid Viewer, EntityUid Actor), int> Viewers = new();
        public readonly HashSet<EntityUid> Subscribed = new();
        public bool Dirty;
    }

    private readonly Dictionary<ICommonSession, ViewerSession> _viewerSessions = new();
    private readonly Dictionary<EntityUid, ICommonSession> _actorSessions = new();
    private readonly HashSet<EntityUid> _selfTracked = new();
    private readonly HashSet<EntityUid> _refreshedViewers = new();
    private readonly HashSet<EntityUid> _desiredBuffer = new();
    private readonly List<EntityUid> _resolveBuffer = new();
    private readonly List<(EntityUid Viewer, EntityUid Actor)> _viewerRemoveBuffer = new();
    private bool _anySessionDirty;

    private void InitializeViewers()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, MapInitEvent>(OnUserMapInit);
        SubscribeLocalEvent<TacticalMapUserComponent, SquadMemberUpdatedEvent>(OnViewerAccessChanged);
        SubscribeLocalEvent<TacticalMapUserComponent, SquadMemberRemovedEvent>(OnViewerAccessChanged);
        SubscribeLocalEvent<TacticalMapUserComponent, HiveChangedEvent>(OnViewerAccessChanged);
        SubscribeLocalEvent<TacticalMapUserComponent, DidEquipEvent>(OnViewerAccessChanged);
        SubscribeLocalEvent<TacticalMapUserComponent, DidUnequipEvent>(OnViewerAccessChanged);

        SubscribeLocalEvent<TacticalMapComputerComponent, BoundUIOpenedEvent>(OnComputerUIOpened);
        SubscribeLocalEvent<TacticalMapComputerComponent, BoundUIClosedEvent>(OnComputerUIClosed);

        SubscribeLocalEvent<XenoOvipositorChangedEvent>(OnOvipositorChanged);
        SubscribeLocalEvent<GhostComponent, TacticalMapPointActivatedEvent>(OnGhostPointActivated);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        Subs.BuiEvents<TacticalMapUserComponent>(TacticalMapUserUi.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUserUIOpened);
                subs.Event<BoundUIClosedEvent>(OnUserUIClosed);
                subs.Event<TacticalMapSubmitCanvasMsg>(OnUserSubmitCanvas);
                subs.Event<TacticalMapActivateBlipMsg>(OnUserActivateBlip);
                subs.Event<TacticalMapActivatePointMsg>(OnUserActivatePoint);
            });

        Subs.BuiEvents<TacticalMapComputerComponent>(TacticalMapComputerUi.Key,
            subs =>
            {
                subs.Event<TacticalMapSubmitCanvasMsg>(OnComputerSubmitCanvas);
                subs.Event<TacticalMapActivateBlipMsg>(OnComputerActivateBlip);
                subs.Event<TacticalMapActivatePointMsg>(OnComputerActivatePoint);
            });

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void ShutdownViewers()
    {
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnUserMapInit(Entity<TacticalMapUserComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnViewerAccessChanged<T>(Entity<TacticalMapUserComponent> ent, ref T args)
    {
        MarkViewerDirty(ent);
    }

    private void OnUserUIOpened(Entity<TacticalMapUserComponent> ent, ref BoundUIOpenedEvent args)
    {
        OpenViewer(ent, args.Actor);
    }

    private void OnUserUIClosed(Entity<TacticalMapUserComponent> ent, ref BoundUIClosedEvent args)
    {
        CloseViewer(ent, args.Actor);
    }

    private void OnComputerUIOpened(Entity<TacticalMapComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.UiKeys.Contains(args.UiKey))
            OpenViewer(ent, args.Actor);
    }

    private void OnComputerUIClosed(Entity<TacticalMapComputerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.UiKeys.Contains(args.UiKey))
            CloseViewer(ent, args.Actor);
    }

    private void OnOvipositorChanged(ref XenoOvipositorChangedEvent ev)
    {
        MarkAllSessionsDirty();
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        _selfTracked.Remove(ev.Entity);
        _actorSessions.Remove(ev.Entity);
        if (!_viewerSessions.TryGetValue(ev.Player, out var state))
            return;

        _viewerRemoveBuffer.Clear();
        foreach (var key in state.Viewers.Keys)
        {
            if (key.Actor == ev.Entity)
                _viewerRemoveBuffer.Add(key);
        }

        foreach (var key in _viewerRemoveBuffer)
        {
            state.Viewers.Remove(key);
        }

        state.Dirty = true;
        _anySessionDirty = true;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected)
            return;

        _viewerSessions.Remove(e.Session);
    }

    public override void OpenViewer(EntityUid viewer, EntityUid actor)
    {
        if (!TryComp(actor, out ActorComponent? actorComp))
            return;

        var session = actorComp.PlayerSession;
        _actorSessions[actor] = session;
        if (!_viewerSessions.TryGetValue(session, out var state))
        {
            state = new ViewerSession();
            _viewerSessions[session] = state;
        }

        var key = (viewer, actor);
        state.Viewers[key] = state.Viewers.GetValueOrDefault(key) + 1;

        if (viewer == actor && HasComp<TacticalMapUserComponent>(viewer))
        {
            _selfTracked.Add(viewer);
            if (TryComp(viewer, out ActiveTacticalMapTrackedComponent? active))
                UpdateSelf(viewer, active.Map, active.Indices);
        }

        state.Dirty = true;
        _anySessionDirty = true;
    }

    public override void CloseViewer(EntityUid viewer, EntityUid actor)
    {
        if (!_actorSessions.TryGetValue(actor, out var session) ||
            !_viewerSessions.TryGetValue(session, out var state))
        {
            return;
        }

        var key = (viewer, actor);
        if (!state.Viewers.TryGetValue(key, out var count))
            return;

        if (count <= 1)
        {
            state.Viewers.Remove(key);
            if (viewer == actor)
                _selfTracked.Remove(viewer);
        }
        else
        {
            state.Viewers[key] = count - 1;
        }

        state.Dirty = true;
        _anySessionDirty = true;
    }

    private void MarkViewerDirty(EntityUid viewer)
    {
        foreach (var state in _viewerSessions.Values)
        {
            foreach (var key in state.Viewers.Keys)
            {
                if (key.Viewer != viewer)
                    continue;

                state.Dirty = true;
                _anySessionDirty = true;
                break;
            }
        }
    }

    private void MarkAllSessionsDirty()
    {
        foreach (var state in _viewerSessions.Values)
        {
            state.Dirty = true;
        }

        _anySessionDirty = _viewerSessions.Count > 0;
    }

    private void RefreshDirtySessions()
    {
        if (!_anySessionDirty)
            return;

        _anySessionDirty = false;
        _refreshedViewers.Clear();
        foreach (var (session, state) in _viewerSessions)
        {
            if (!state.Dirty)
                continue;

            state.Dirty = false;
            RefreshSession(session, state);
        }
    }

    private void RefreshSession(ICommonSession session, ViewerSession state)
    {
        _desiredBuffer.Clear();
        _viewerRemoveBuffer.Clear();

        foreach (var key in state.Viewers.Keys)
        {
            if (TerminatingOrDeleted(key.Viewer))
            {
                _viewerRemoveBuffer.Add(key);
                continue;
            }

            RefreshViewer(key.Viewer);
            if (!TryGetViewer(key.Viewer, out var channels, out var live))
                continue;

            foreach (var channel in channels)
            {
                _desiredBuffer.Add(channel);
                if (live && TryComp(channel, out TacticalMapChannelComponent? comp) && comp.LiveFeed is { } feed)
                    _desiredBuffer.Add(feed);
            }
        }

        foreach (var key in _viewerRemoveBuffer)
        {
            state.Viewers.Remove(key);
        }

        foreach (var subscribed in state.Subscribed)
        {
            if (!_desiredBuffer.Contains(subscribed) && !TerminatingOrDeleted(subscribed))
                _pvsOverride.RemoveSessionOverride(subscribed, session);
        }

        foreach (var desired in _desiredBuffer)
        {
            if (!state.Subscribed.Contains(desired))
                _pvsOverride.AddSessionOverride(desired, session);
        }

        state.Subscribed.Clear();
        state.Subscribed.UnionWith(_desiredBuffer);
    }

    private void RefreshViewer(EntityUid viewer)
    {
        if (!_refreshedViewers.Add(viewer))
            return;

        var access = CollectAccess(viewer);
        _resolveBuffer.Clear();
        ResolveChannels(access, _resolveBuffer);

        if (TryComp(viewer, out TacticalMapComputerComponent? computer))
        {
            if (computer.Live == access.Live && computer.Channels.SequenceEqual(_resolveBuffer))
                return;

            computer.Channels = new List<EntityUid>(_resolveBuffer);
            computer.Live = access.Live;
            Dirty(viewer, computer);
        }
        else if (TryComp(viewer, out TacticalMapUserComponent? user))
        {
            if (user.Live == access.Live && user.Channels.SequenceEqual(_resolveBuffer))
                return;

            user.Channels = new List<EntityUid>(_resolveBuffer);
            user.Live = access.Live;
            Dirty(viewer, user);
        }
    }

    private void ResolveChannels(TacticalMapCollectAccessEvent access, List<EntityUid> result)
    {
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var mapUid, out var mapComp))
        {
            var map = (mapUid, mapComp);
            foreach (var (id, proto) in _channelPrototypes)
            {
                var factionAllowed = access.AllChannels || access.Factions.Contains(proto.Faction);
                if (!factionAllowed)
                    continue;

                switch (proto.Scope)
                {
                    case TacticalMapChannelScope.Faction:
                        result.Add(GetOrCreateChannel(map, id, null));
                        break;
                    case TacticalMapChannelScope.Hive:
                        if (access.AllChannels)
                        {
                            var hives = EntityQueryEnumerator<HiveComponent>();
                            while (hives.MoveNext(out var hive, out _))
                            {
                                result.Add(GetOrCreateChannel(map, id, hive));
                            }
                        }
                        else
                        {
                            foreach (var hive in access.Hives)
                            {
                                result.Add(GetOrCreateChannel(map, id, hive));
                            }
                        }

                        break;
                    case TacticalMapChannelScope.Squad:
                        if (access.AllChannels || access.AllSquads)
                        {
                            var squads = EntityQueryEnumerator<SquadTeamComponent>();
                            while (squads.MoveNext(out var squad, out var squadComp))
                            {
                                if (IsSquadAllowed(proto, access, squadComp))
                                    result.Add(GetOrCreateChannel(map, id, squad));
                            }
                        }
                        else
                        {
                            foreach (var squad in access.Squads)
                            {
                                if (TryComp(squad, out SquadTeamComponent? squadComp) &&
                                    IsSquadAllowed(proto, access, squadComp))
                                {
                                    result.Add(GetOrCreateChannel(map, id, squad));
                                }
                            }
                        }

                        break;
                }
            }
        }
    }

    private static bool IsSquadAllowed(TacticalMapChannelComponent proto, TacticalMapCollectAccessEvent access, SquadTeamComponent squad)
    {
        if (proto.SquadGroups.Count > 0 && !proto.SquadGroups.Contains(squad.Group))
            return false;

        if (access.AllChannels || access.SquadGroups.Count == 0)
            return true;

        return access.SquadGroups.Contains(squad.Group);
    }

    private void OnUserSubmitCanvas(Entity<TacticalMapUserComponent> ent, ref TacticalMapSubmitCanvasMsg args)
    {
        SubmitCanvas(ent, args.Actor, args);
    }

    private void OnComputerSubmitCanvas(Entity<TacticalMapComputerComponent> ent, ref TacticalMapSubmitCanvasMsg args)
    {
        SubmitCanvas(ent, args.Actor, args);
    }

    private void SubmitCanvas(EntityUid viewer, EntityUid actor, TacticalMapSubmitCanvasMsg args)
    {
        if (!TryGetEntity(args.Channel, out var channelId) ||
            !CanDraw(actor, viewer, channelId.Value) ||
            !TryComp(channelId, out TacticalMapChannelComponent? channel) ||
            !TryComp(channelId, out TacticalMapCanvasComponent? canvas) ||
            channel.Map is not { } mapId ||
            !TryComp(mapId, out TacticalMapComponent? map))
        {
            return;
        }

        var now = _timing.CurTime;
        if (now < canvas.NextSubmitAt)
            return;

        var strokes = SanitizeStrokes(map, args.Strokes);
        var labels = SanitizeLabels(map, args.Labels);

        canvas.Strokes = strokes;
        canvas.Labels = labels;
        canvas.Version++;
        canvas.LastSubmitAt = now;
        canvas.NextSubmitAt = now + (channel.Cooldown ?? _announceCooldown);
        Dirty(channelId.Value, canvas);

        if (channel.SnapshotOnSubmit)
            TakeSnapshot(channelId.Value, channel);

        _adminLog.Add(LogType.RMCTacticalMapUpdated,
            $"{ToPrettyString(actor):player} updated the {Name(channelId.Value)} tactical map for {ToPrettyString(mapId)}");

        var ev = new TacticalMapCanvasSubmittedEvent(channelId.Value, mapId, actor, strokes, labels);
        RaiseLocalEvent(channelId.Value, ref ev, true);

        _replay.RecordCanvas(mapId, channelId.Value, strokes, labels);
    }

    private List<TacticalMapStroke> SanitizeStrokes(TacticalMapComponent map, List<TacticalMapStroke>? input)
    {
        var result = new List<TacticalMapStroke>();
        if (input == null)
            return result;

        var budget = LineLimit;
        for (var i = input.Count - 1; i >= 0 && budget > 0; i--)
        {
            var stroke = input[i];
            if (stroke.Points is not { Count: > 0 } points)
                continue;

            var take = Math.Min(points.Count, budget);
            var clean = new List<Vector2>(take);
            for (var p = points.Count - take; p < points.Count; p++)
            {
                var point = points[p];
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                    continue;

                clean.Add(ClampToBounds(map, point));
            }

            if (clean.Count == 0)
                continue;

            budget -= clean.Count;
            var color = stroke.Color.WithAlpha(Math.Clamp(stroke.Color.A, 0.1f, 1f));
            var thickness = float.IsFinite(stroke.Thickness)
                ? Math.Clamp(stroke.Thickness, MinStrokeThickness, MaxStrokeThickness)
                : MinStrokeThickness;

            result.Add(new TacticalMapStroke(color, thickness, stroke.Smooth, clean));
        }

        result.Reverse();
        return result;
    }

    private static Vector2 ClampToBounds(TacticalMapComponent map, Vector2 point)
    {
        if (!map.HasBounds)
            return point;

        return new Vector2(
            Math.Clamp(point.X, map.BoundsMin.X - BoundsMargin, map.BoundsMax.X + 1 + BoundsMargin),
            Math.Clamp(point.Y, map.BoundsMin.Y - BoundsMargin, map.BoundsMax.Y + 1 + BoundsMargin));
    }

    private Dictionary<Vector2i, TacticalMapLabelData> SanitizeLabels(
        TacticalMapComponent map,
        Dictionary<Vector2i, TacticalMapLabelData>? input)
    {
        var result = new Dictionary<Vector2i, TacticalMapLabelData>();
        if (input == null)
            return result;

        foreach (var (position, label) in input)
        {
            if (result.Count >= LabelLimit)
                break;

            if (map.HasBounds &&
                (position.X < map.BoundsMin.X - BoundsMargin ||
                 position.Y < map.BoundsMin.Y - BoundsMargin ||
                 position.X > map.BoundsMax.X + BoundsMargin ||
                 position.Y > map.BoundsMax.Y + BoundsMargin))
            {
                continue;
            }

            var text = label.Text?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            if (text.Length > LabelMaxLength)
                text = text.Substring(0, LabelMaxLength);

            result[position] = new TacticalMapLabelData(text, label.Color.WithAlpha(1f));
        }

        return result;
    }

    private void TakeSnapshot(EntityUid channelId, TacticalMapChannelComponent channel)
    {
        if (!TryComp(channelId, out TacticalMapSnapshotComponent? snapshot))
            return;

        snapshot.Blips.Clear();
        if (channel.LiveFeed is { } feedUid && TryComp(feedUid, out TacticalMapLiveFeedComponent? feed))
        {
            foreach (var (net, blip) in feed.Blips)
            {
                snapshot.Blips[net] = blip;
            }
        }

        snapshot.TakenAt = _timing.CurTime;
        Dirty(channelId, snapshot);
    }

    private void OnUserActivateBlip(Entity<TacticalMapUserComponent> ent, ref TacticalMapActivateBlipMsg args)
    {
        ActivateBlip(ent, args.Actor, args.Target);
    }

    private void OnComputerActivateBlip(Entity<TacticalMapComputerComponent> ent, ref TacticalMapActivateBlipMsg args)
    {
        ActivateBlip(ent, args.Actor, args.Target);
    }

    private void ActivateBlip(EntityUid viewer, EntityUid actor, NetEntity targetNet)
    {
        if (!TryGetEntity(targetNet, out var target) ||
            !TryGetViewer(viewer, out var channels, out var live) ||
            !IsBlipVisible(channels, live, targetNet))
        {
            return;
        }

        var ev = new TacticalMapBlipActivatedEvent(viewer, actor, target.Value, live);
        RaiseLocalEvent(viewer, ref ev);
    }

    private bool IsBlipVisible(IReadOnlyList<EntityUid> channels, bool live, NetEntity target)
    {
        foreach (var channel in channels)
        {
            if (live &&
                TryComp(channel, out TacticalMapChannelComponent? comp) &&
                comp.LiveFeed is { } feedUid &&
                TryComp(feedUid, out TacticalMapLiveFeedComponent? feed) &&
                feed.Blips.ContainsKey(target))
            {
                return true;
            }

            if (TryComp(channel, out TacticalMapSnapshotComponent? snapshot) && snapshot.Blips.ContainsKey(target))
                return true;
        }

        return false;
    }

    private void OnUserActivatePoint(Entity<TacticalMapUserComponent> ent, ref TacticalMapActivatePointMsg args)
    {
        ActivatePoint(ent, args.Actor, args.Map, args.Indices);
    }

    private void OnComputerActivatePoint(Entity<TacticalMapComputerComponent> ent, ref TacticalMapActivatePointMsg args)
    {
        ActivatePoint(ent, args.Actor, args.Map, args.Indices);
    }

    private void ActivatePoint(EntityUid viewer, EntityUid actor, NetEntity mapNet, Vector2i indices)
    {
        if (!TryGetEntity(mapNet, out var map) ||
            !HasComp<TacticalMapComponent>(map) ||
            !TryGetViewer(viewer, out var channels, out _))
        {
            return;
        }

        var onMap = false;
        foreach (var channel in channels)
        {
            if (TryComp(channel, out TacticalMapChannelComponent? comp) && comp.Map == map)
            {
                onMap = true;
                break;
            }
        }

        if (!onMap)
            return;

        var ev = new TacticalMapPointActivatedEvent(viewer, map.Value, indices);
        RaiseLocalEvent(actor, ref ev);
    }

    private void OnGhostPointActivated(Entity<GhostComponent> ent, ref TacticalMapPointActivatedEvent args)
    {
        if (args.Handled || !TryComp(args.Map, out MapGridComponent? grid))
            return;

        args.Handled = true;
        var position = new Vector2(args.Indices.X + 0.5f, args.Indices.Y + 0.5f) * grid.TileSize;
        _transform.SetCoordinates(ent, new EntityCoordinates(args.Map, position));
        _transform.AttachToGridOrMap(ent);

        if (TryComp(ent, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);
    }
}
