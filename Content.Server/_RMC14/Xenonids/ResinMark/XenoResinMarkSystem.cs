using Content.Server._RMC14.Xenonids.Ping;
using Content.Server._RMC14.Xenonids.Watch;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RMC14.Tracker.Xeno;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Ping;
using Content.Shared._RMC14.Xenonids.ResinMark;
using Content.Shared._RMC14.Xenonids.Word;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Server.GameStates;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Content.Server._RMC14.Xenonids.ResinMark;

public sealed class XenoResinMarkSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly ResinMarkerTrackerSystem _resinMarkerTracker = default!;
    [Dependency] private readonly XenoPingSystem _ping = default!;
    [Dependency] private readonly XenoWatchSystem _xenoWatch = default!;

    private readonly HashSet<EntityUid> _pendingUiRefreshHives = new();
    private static readonly Vector2 MarkerPingOffset = new(0f, 0.3f);

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoResinMarkComponent, XenoResinMarkActionEvent>(OnMarkAction);
        SubscribeLocalEvent<XenoResinMarkerComponent, ComponentShutdown>(OnMarkerShutdown);
        SubscribeLocalEvent<XenoResinMarkerComponent, RequestTrackableNameEvent>(OnRequestTrackableName);
        SubscribeLocalEvent<XenoResinMarkWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);
        SubscribeLocalEvent<XenoResinMarkWatchingComponent, ComponentShutdown>(OnWatchingShutdown);
        SubscribeLocalEvent<XenoResinMarkWatchingComponent, EntityTerminatingEvent>(OnWatchingTerminating);
        SubscribeNetworkEvent<XenoResinMarkPlaceRequestEvent>(OnPlaceMarkRequest);

        Subs.BuiEvents<XenoResinMarkComponent>(XenoResinMarkUIKey.Key, subs =>
        {
            subs.Event<XenoResinMarkSelectTypeBuiMsg>(OnSelectMarkType);
            subs.Event<XenoResinMarkWatchBuiMsg>(OnWatchMark);
            subs.Event<XenoResinMarkDestroyBuiMsg>(OnDestroyMark);
            subs.Event<XenoResinMarkForceTrackBuiMsg>(OnForceTrackMark);
        });
    }

    private void OnMarkAction(Entity<XenoResinMarkComponent> ent, ref XenoResinMarkActionEvent args)
    {
        args.Handled = true;

        if (_hive.GetHive(ent.Owner) is not { } hive)
        {
            _popup.PopupEntity("You must be in a hive to place marks.", ent, ent, PopupType.SmallCaution);
            return;
        }

        _ui.TryOpenUi(ent.Owner, XenoResinMarkUIKey.Key, ent);
        RefreshUi(ent, hive);
    }

    private void OnSelectMarkType(Entity<XenoResinMarkComponent> ent, ref XenoResinMarkSelectTypeBuiMsg args)
    {
        if (!TryGetMarkType(args.Type, out _))
            return;

        ent.Comp.SelectedPingType = args.Type;
        ent.Comp.PlacementEnabled = true;
        Dirty(ent);

        if (_hive.GetHive(ent.Owner) is { } hive)
            RefreshUi(ent, hive);
    }

    private void OnPlaceMarkRequest(XenoResinMarkPlaceRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player ||
            !TryComp<XenoResinMarkComponent>(player, out var markComp))
        {
            return;
        }

        TryPlaceMarkAtCoordinates((player, markComp), GetCoordinates(msg.Coordinates));
    }

    private bool TryPlaceMarkAtCoordinates(Entity<XenoResinMarkComponent> ent, EntityCoordinates coordinates)
    {
        if (!ent.Comp.PlacementEnabled)
        {
            _popup.PopupEntity("Select a marker in the Mark Resin menu first.", ent, ent, PopupType.SmallCaution);
            return false;
        }

        if (!TryCanPlaceMark(ent, out var hive))
            return false;

        if (!IsValidCoordinates(coordinates))
        {
            _popup.PopupEntity("Invalid target location.", ent, ent, PopupType.SmallCaution);
            return false;
        }

        if (!TryGetMarkType(ent.Comp.SelectedPingType, out var pingData))
        {
            _popup.PopupEntity("Invalid mark type selected.", ent, ent, PopupType.SmallCaution);
            return false;
        }

        var marker = Spawn(ent.Comp.MarkerPrototype, coordinates);
        var markerComp = EnsureComp<XenoResinMarkerComponent>(marker);
        markerComp.Creator = ent.Owner;
        markerComp.Hive = hive.Owner;
        markerComp.PingType = ent.Comp.SelectedPingType;

        var ping = Spawn(ent.Comp.SelectedPingType, coordinates.Offset(MarkerPingOffset));
        if (TryComp<XenoPingEntityComponent>(ping, out var pingComp))
        {
            pingComp.PingType = ent.Comp.SelectedPingType;
            pingComp.Creator = ent.Owner;
            pingComp.Hive = hive.Owner;
            pingComp.Lifetime = ent.Comp.PingLifetime;
            pingComp.DeleteAt = _timing.CurTime + ent.Comp.PingLifetime;
            pingComp.AttachedTarget = marker;
            pingComp.LastKnownCoordinates = coordinates;
            pingComp.WorldPosition = Transform(marker).MapPosition.Position;
            pingComp.ShowWaypoint = false;
            pingComp.AttachedOffset = MarkerPingOffset;
            Dirty(ping, pingComp);
        }

        markerComp.LinkedPing = ping;
        Dirty(marker, markerComp);

        AddHivePvsOverrides(marker, hive.Owner);
        AddHivePvsOverrides(ping, hive.Owner);

        _ping.SendRoleBasedPingCallout(ent.Owner, ping, pingData, coordinates, hive);

        ent.Comp.LastPlacedAt = _timing.CurTime;
        Dirty(ent);
        _popup.PopupEntity($"Placed {pingData.Name} mark.", ent, ent, PopupType.Small);

        RefreshUi(ent, hive);
        return true;
    }

    private void OnWatchMark(Entity<XenoResinMarkComponent> ent, ref XenoResinMarkWatchBuiMsg args)
    {
        if (!TryGetEntity(args.Marker, out var markerUidNullable) ||
            markerUidNullable == null)
        {
            return;
        }

        var markerUid = markerUidNullable.Value;
        if (!TryComp<XenoResinMarkerComponent>(markerUid, out var markerComp) ||
            _hive.GetHive(ent.Owner) is not { } hive ||
            markerComp.Hive != hive.Owner)
        {
            return;
        }

        if (!TryComp<ActorComponent>(ent.Owner, out var actor) ||
            !TryComp<EyeComponent>(ent.Owner, out _))
        {
            return;
        }

        if (TryComp<XenoWatchingComponent>(ent.Owner, out _))
            _xenoWatch.Unwatch(ent.Owner, actor.PlayerSession);

        UnwatchMarker(ent.Owner, actor.PlayerSession);

        _eye.SetTarget(ent.Owner, markerUid);
        _viewSubscriber.AddViewSubscriber(markerUid, actor.PlayerSession);

        var watching = EnsureComp<XenoResinMarkWatchingComponent>(ent.Owner);
        watching.Marker = markerUid;
        markerComp.Watchers.Add(ent.Owner);
        Dirty(markerUid, markerComp);
    }

    private void OnDestroyMark(Entity<XenoResinMarkComponent> ent, ref XenoResinMarkDestroyBuiMsg args)
    {
        if (!TryGetEntity(args.Marker, out var markerUidNullable) ||
            markerUidNullable == null)
        {
            return;
        }

        var markerUid = markerUidNullable.Value;
        if (!TryComp<XenoResinMarkerComponent>(markerUid, out var markerComp) ||
            _hive.GetHive(ent.Owner) is not { } hive ||
            markerComp.Hive != hive.Owner)
        {
            return;
        }

        QueueDel(markerUid);
        QueueUiRefreshForHive(hive.Owner);
    }

    private void OnForceTrackMark(Entity<XenoResinMarkComponent> ent, ref XenoResinMarkForceTrackBuiMsg args)
    {
        if (!HasComp<XenoWordQueenComponent>(ent.Owner))
        {
            _popup.PopupEntity("Only the Queen can force marker tracking.", ent, ent, PopupType.SmallCaution);
            return;
        }

        if (!TryGetEntity(args.Marker, out var markerUidNullable) ||
            markerUidNullable == null)
        {
            return;
        }

        var markerUid = markerUidNullable.Value;
        if (!TryComp<XenoResinMarkerComponent>(markerUid, out var markerComp) ||
            _hive.GetHive(ent.Owner) is not { } hive ||
            markerComp.Hive != hive.Owner)
        {
            return;
        }

        var trackerQuery = EntityQueryEnumerator<HiveMemberComponent, ResinMarkerTrackerComponent>();
        while (trackerQuery.MoveNext(out var uid, out var member, out _))
        {
            if (member.Hive != hive.Owner)
                continue;

            _resinMarkerTracker.ForceTrackTarget(uid, markerUid);
        }

        _popup.PopupEntity("Forced the hive to track this marker.", ent, ent, PopupType.Small);
    }

    private void OnMarkerShutdown(Entity<XenoResinMarkerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var watcherUid in ent.Comp.Watchers.ToArray())
        {
            if (!TryComp<ActorComponent>(watcherUid, out var actor))
                continue;

            UnwatchMarker(watcherUid, actor.PlayerSession);
        }

        if (ent.Comp.LinkedPing is { } linkedPing && Exists(linkedPing))
            QueueDel(linkedPing);

        RemovePvsOverrides(ent.Owner);

        QueueUiRefreshForHive(ent.Comp.Hive);
    }

    private void OnRequestTrackableName(Entity<XenoResinMarkerComponent> ent, ref RequestTrackableNameEvent args)
    {
        if (args.Handled)
            return;

        var typeName = ent.Comp.PingType;
        if (TryGetMarkType(ent.Comp.PingType, out var pingData))
            typeName = pingData.Name;

        var locationName = _areas.TryGetArea(Transform(ent).Coordinates, out _, out var area) ? area.Name : "Unknown Area";
        args.Name = $"{typeName} ({locationName})";
        args.Handled = true;
    }

    private void OnWatchingMoveInput(Entity<XenoResinMarkWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (TryComp<ActorComponent>(ent.Owner, out var actor))
            UnwatchMarker(ent.Owner, actor.PlayerSession);
    }

    private void OnWatchingShutdown(Entity<XenoResinMarkWatchingComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<ActorComponent>(ent.Owner, out var actor))
            UnwatchMarker(ent.Owner, actor.PlayerSession);
    }

    private void OnWatchingTerminating(Entity<XenoResinMarkWatchingComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp<ActorComponent>(ent.Owner, out var actor))
            UnwatchMarker(ent.Owner, actor.PlayerSession);
    }

    private bool TryCanPlaceMark(Entity<XenoResinMarkComponent> ent, out Entity<HiveComponent> hive)
    {
        hive = default;

        if (_hive.GetHive(ent.Owner) is not { } hiveEnt)
            return false;

        var nextPlaceAt = ent.Comp.LastPlacedAt + ent.Comp.Cooldown;
        if (_timing.CurTime < nextPlaceAt)
        {
            var remaining = nextPlaceAt - _timing.CurTime;
            _popup.PopupEntity($"Mark is on cooldown for {Math.Ceiling(remaining.TotalSeconds)}s.", ent, ent, PopupType.SmallCaution);
            return false;
        }

        hive = hiveEnt;
        return true;
    }

    private void RefreshUi(Entity<XenoResinMarkComponent> ent, Entity<HiveComponent> hive)
    {
        if (_net.IsClient)
            return;

        var canForceTrack = HasComp<XenoWordQueenComponent>(ent.Owner);
        var types = new List<XenoResinMarkType>();
        foreach (var (id, data) in GetAvailableMarkTypes())
        {
            types.Add(new XenoResinMarkType(id, data.Name, data.Description));
        }

        if (types.Count == 0)
        {
            _ui.SetUiState(ent.Owner, XenoResinMarkUIKey.Key,
                new XenoResinMarkBuiState(ent.Comp.SelectedPingType, types, new List<XenoResinPlacedMark>(), canForceTrack));
            return;
        }

        if (!types.Any(t => t.Id == ent.Comp.SelectedPingType))
        {
            ent.Comp.SelectedPingType = types[0].Id;
            Dirty(ent);
        }

        var marks = new List<XenoResinPlacedMark>();
        var query = EntityQueryEnumerator<XenoResinMarkerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var markerComp, out var xform))
        {
            if (markerComp.Hive != hive.Owner)
                continue;

            var name = TryGetMarkType(markerComp.PingType, out var pingData) ? pingData.Name : markerComp.PingType;
            var locationName = _areas.TryGetArea(xform.Coordinates, out _, out var area) ? area.Name : "Unknown Area";
            marks.Add(new XenoResinPlacedMark(GetNetEntity(uid), name, locationName));
        }

        marks.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        _ui.SetUiState(ent.Owner, XenoResinMarkUIKey.Key, new XenoResinMarkBuiState(ent.Comp.SelectedPingType, types, marks, canForceTrack));
    }

    private Dictionary<EntProtoId, XenoPingDataComponent> GetAvailableMarkTypes()
    {
        var marks = new Dictionary<EntProtoId, XenoPingDataComponent>();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.TryGetComponent<XenoPingDataComponent>(out var pingData, EntityManager.ComponentFactory) ||
                pingData == null ||
                pingData.IsConstruction)
            {
                continue;
            }

            marks[proto.ID] = pingData;
        }

        return marks
            .OrderByDescending(kvp => kvp.Value.Priority)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private bool TryGetMarkType(EntProtoId pingType, out XenoPingDataComponent pingData)
    {
        pingData = default!;

        if (!_prototype.TryIndex(pingType, out var prototype))
            return false;

        if (!prototype.TryGetComponent(out XenoPingDataComponent? data, EntityManager.ComponentFactory) ||
            data == null)
        {
            return false;
        }

        pingData = data;
        return true;
    }

    private bool TryGetMarkType(string pingType, out XenoPingDataComponent pingData)
    {
        pingData = default!;

        if (!_prototype.TryIndex<EntityPrototype>(pingType, out var prototype))
            return false;

        if (!prototype.TryGetComponent(out XenoPingDataComponent? data, EntityManager.ComponentFactory) ||
            data == null)
        {
            return false;
        }

        pingData = data;
        return true;
    }

    private void AddHivePvsOverrides(EntityUid uid, EntityUid hiveId)
    {
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } playerEntity)
                continue;

            if (!_hive.IsMember(playerEntity, hiveId))
                continue;

            _pvsOverride.AddSessionOverride(uid, session);
        }
    }

    private void RemovePvsOverrides(EntityUid uid)
    {
        foreach (var session in _players.Sessions)
        {
            _pvsOverride.RemoveSessionOverride(uid, session);
        }
    }

    private void RefreshUiForHive(EntityUid hiveUid)
    {
        if (!TryComp(hiveUid, out HiveComponent? hiveComp))
            return;

        var hive = (hiveUid, hiveComp);
        var query = EntityQueryEnumerator<XenoResinMarkComponent, HiveMemberComponent>();
        while (query.MoveNext(out var uid, out var markComp, out var memberComp))
        {
            if (memberComp.Hive != hiveUid)
                continue;

            RefreshUi((uid, markComp), hive);
        }
    }

    private void QueueUiRefreshForHive(EntityUid hiveUid)
    {
        if (hiveUid != EntityUid.Invalid)
            _pendingUiRefreshHives.Add(hiveUid);
    }

    public override void Update(float frameTime)
    {
        if (_pendingUiRefreshHives.Count == 0)
            return;

        foreach (var hiveUid in _pendingUiRefreshHives)
        {
            RefreshUiForHive(hiveUid);
        }

        _pendingUiRefreshHives.Clear();
    }

    private void UnwatchMarker(EntityUid watcher, ICommonSession session)
    {
        if (!TryComp<XenoResinMarkWatchingComponent>(watcher, out var watchingComp))
            return;

        if (watchingComp.Marker is { } marker &&
            Exists(marker))
        {
            _viewSubscriber.RemoveViewSubscriber(marker, session);

            if (TryComp<XenoResinMarkerComponent>(marker, out var markerComp))
            {
                markerComp.Watchers.Remove(watcher);
                Dirty(marker, markerComp);
            }
        }

        _eye.SetTarget(watcher, null);
        RemCompDeferred<XenoResinMarkWatchingComponent>(watcher);
    }

    private bool IsValidCoordinates(EntityCoordinates coordinates)
    {
        return coordinates.IsValid(EntityManager) && coordinates.GetMapId(EntityManager) != MapId.Nullspace;
    }

    private EntityCoordinates GetCoordinates(NetCoordinates netCoordinates)
    {
        if (!TryGetEntity(netCoordinates.NetEntity, out var entityUid))
            return EntityCoordinates.Invalid;

        return new EntityCoordinates(entityUid.Value, netCoordinates.Position);
    }
}
