using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Word;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using System;
using System.Linq;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Xenonids.Ping;

public abstract class SharedXenoPingSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedCMChatSystem _cmChat = default!;
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const double MinPingDelaySeconds = 2.0;
    private const int NormalXenoPingLimit = 3;
    private const int HiveLeaderPingLimit = 8;
    private const float AttachedTargetPositionThreshold = 0.01f;
    private const double QueenPingLifetimeSeconds = 40.0;
    private const double HiveLeaderPingLifetimeSeconds = 25.0;
    private const double NormalPingLifetimeSeconds = 8.0;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    private readonly Dictionary<EntityUid, TimeSpan> _lastPingTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var currentTime = _timing.CurTime;
        UpdateAttachedTargetPositions(currentTime);
        CleanupExpiredPings(currentTime);
    }

    private void UpdateAttachedTargetPositions(TimeSpan currentTime)
    {
        var pingsToUpdate = new List<(EntityUid uid, XenoPingEntityComponent ping)>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (ping.AttachedTarget.HasValue)
            {
                pingsToUpdate.Add((uid, ping));
            }
        }

        foreach (var (uid, ping) in pingsToUpdate)
        {
            UpdateSinglePingPosition(uid, ping);
        }
    }

    private void UpdateSinglePingPosition(EntityUid pingUid, XenoPingEntityComponent ping)
    {
        if (!ping.AttachedTarget.HasValue)
            return;

        var targetEntity = ping.AttachedTarget.Value;

        if (!Exists(targetEntity))
        {
            HandleDetachedTarget(pingUid, ping);
            return;
        }

        if (!_transformQuery.TryGetComponent(targetEntity, out var targetTransform))
            return;

        var targetWorldPos = _transform.GetWorldPosition(targetEntity);
        var currentWorldPos = _transform.GetWorldPosition(pingUid);
        var distanceMoved = Vector2.Distance(targetWorldPos, currentWorldPos);

        if (distanceMoved > AttachedTargetPositionThreshold)
        {
            UpdatePingPosition(pingUid, ping, targetTransform.Coordinates, targetWorldPos);
        }
    }

    private void HandleDetachedTarget(EntityUid pingUid, XenoPingEntityComponent ping)
    {
        if (ping.LastKnownCoordinates.HasValue)
        {
            _transform.SetCoordinates(pingUid, ping.LastKnownCoordinates.Value);
        }

        ping.AttachedTarget = null;
        Dirty(pingUid, ping);
    }

    private void UpdatePingPosition(EntityUid pingUid, XenoPingEntityComponent ping, EntityCoordinates targetCoordinates, Vector2 targetWorldPos)
    {
        ping.LastKnownCoordinates = targetCoordinates;
        ping.WorldPosition = targetWorldPos;
        _transform.SetCoordinates(pingUid, targetCoordinates);
        Dirty(pingUid, ping);
    }

    private void CleanupExpiredPings(TimeSpan currentTime)
    {
        var toDelete = new List<EntityUid>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (currentTime >= ping.DeleteAt)
            {
                toDelete.Add(uid);
            }
        }

        foreach (var uid in toDelete)
        {
            QueueDel(uid);
        }
    }

    protected void OnPingRequest(Entity<XenoPingComponent> ent, ref XenoPingRequestEvent args)
    {
        if (_net.IsClient)
            return;

        if (!ValidatePingCooldown(ent.Owner))
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!IsValidCoordinates(coordinates))
            return;

        var targetEntity = ResolveTargetEntity(args.TargetEntity);
        CreatePing(ent.Owner, args.PingType, coordinates, targetEntity);
    }

    private bool ValidatePingCooldown(EntityUid creator)
    {
        var currentTime = _timing.CurTime;

        if (_lastPingTimes.TryGetValue(creator, out var lastPing))
        {
            var timeSinceLastPing = (currentTime - lastPing).TotalSeconds;
            if (timeSinceLastPing < MinPingDelaySeconds)
            {
                _popup.PopupEntity("You must wait before pinging again.", creator, creator, PopupType.SmallCaution);
                return false;
            }
        }

        _lastPingTimes[creator] = currentTime;
        return true;
    }

    protected bool IsValidCoordinates(EntityCoordinates coordinates)
    {
        return coordinates.IsValid(EntityManager) && _mapManager.MapExists(coordinates.GetMapId(EntityManager));
    }

    protected EntityUid? ResolveTargetEntity(NetEntity? targetNetEntity)
    {
        if (!targetNetEntity.HasValue)
            return null;

        return TryGetEntity(targetNetEntity.Value, out var entityUid) ? entityUid : null;
    }

    protected void CreatePing(EntityUid creator, string pingEntityId, EntityCoordinates coordinates, EntityUid? targetEntity = null)
    {
        if (_net.IsClient)
            return;

        if (!ValidateXenoCreator(creator, out var hive))
            return;

        if (!_prototypeManager.TryIndex<EntityPrototype>(pingEntityId, out var entityProto))
        {
            _popup.PopupEntity("Invalid ping type.", creator, creator, PopupType.SmallCaution);
            return;
        }

        if (!entityProto.Components.TryGetValue("XenoPingData", out var pingDataComponent))
        {
            _popup.PopupEntity("Invalid ping configuration.", creator, creator, PopupType.SmallCaution);
            return;
        }

        var pingData = (XenoPingDataComponent)pingDataComponent.Component;
        var creatorRole = DetermineCreatorRole(creator);
        EnforcePingLimits(creator, creatorRole);
        CreatePingBasedOnRole(creator, pingEntityId, pingData, coordinates, hive, targetEntity, creatorRole);
    }

    private bool ValidateXenoCreator(EntityUid creator, out Entity<HiveComponent> hive)
    {
        hive = default;

        if (!_xenoQuery.HasComponent(creator))
            return false;

        var hiveEntity = _hive.GetHive(creator);
        if (hiveEntity == null)
        {
            _popup.PopupEntity("You must be in a hive to ping.", creator, creator, PopupType.SmallCaution);
            return false;
        }

        hive = hiveEntity.Value;
        return true;
    }

    private CreatorRole DetermineCreatorRole(EntityUid creator)
    {
        if (HasComp<XenoWordQueenComponent>(creator))
            return CreatorRole.Queen;

        if (_hiveLeader.IsLeader(creator, out _))
            return CreatorRole.HiveLeader;

        return CreatorRole.Normal;
    }

    private void CreatePingBasedOnRole(EntityUid creator, string pingEntityId, XenoPingDataComponent pingData,
        EntityCoordinates coordinates, Entity<HiveComponent> hive, EntityUid? targetEntity, CreatorRole role)
    {
        var (lifetime, roleColor, popupType) = role switch
        {
            CreatorRole.Queen => (TimeSpan.FromSeconds(QueenPingLifetimeSeconds), Color.FromHex("#FFD700"), PopupType.Large),
            CreatorRole.HiveLeader => (TimeSpan.FromSeconds(HiveLeaderPingLifetimeSeconds), Color.FromHex("#FF4500"), PopupType.Large),
            _ => (TimeSpan.FromSeconds(NormalPingLifetimeSeconds), (Color?)null, PopupType.Medium)
        };

        CreatePingWithRole(creator, pingEntityId, pingData, coordinates, hive, lifetime, roleColor, popupType, targetEntity);
    }

    private void EnforcePingLimits(EntityUid creator, CreatorRole role)
    {
        if (role == CreatorRole.Queen)
            return;

        var maxPings = role == CreatorRole.HiveLeader ? HiveLeaderPingLimit : NormalXenoPingLimit;
        var creatorPings = CollectCreatorPings(creator);

        if (creatorPings.Count == 0)
            return;

        creatorPings.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
        RemoveExcessPings(creatorPings, maxPings);
    }

    private List<(EntityUid Uid, TimeSpan CreatedAt)> CollectCreatorPings(EntityUid creator)
    {
        var creatorPings = new List<(EntityUid Uid, TimeSpan CreatedAt)>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (ping.Creator == creator)
            {
                var createdAt = ping.DeleteAt - ping.Lifetime;
                creatorPings.Add((uid, createdAt));
            }
        }

        return creatorPings;
    }

    private void RemoveExcessPings(List<(EntityUid Uid, TimeSpan CreatedAt)> pings, int maxPings)
    {
        var pingsToRemove = pings.Count - maxPings + 1;
        for (var i = 0; i < pingsToRemove && i < pings.Count; i++)
        {
            QueueDel(pings[i].Uid);
        }
    }

    private void CreatePingWithRole(EntityUid creator, string pingEntityId, XenoPingDataComponent pingData,
        EntityCoordinates coordinates, Entity<HiveComponent> hive, TimeSpan lifetime, Color? chatColor,
        PopupType popupType, EntityUid? targetEntity = null)
    {
        var ping = CreatePingEntity(creator, pingEntityId, coordinates, lifetime, targetEntity);
        var locationName = GetLocationName(coordinates);
        var creatorName = Name(creator);
        var targetMessage = GetTargetMessage(targetEntity);

        SendHivemindMessage(creator, pingData, locationName, targetMessage, creatorName, chatColor, hive);
        ShowPopupToHiveMembers(creator, pingData, targetMessage, coordinates, hive, popupType);
        PlayPingSound(pingData, ping, hive);
    }

    private EntityUid CreatePingEntity(EntityUid creator, string pingEntityId, EntityCoordinates coordinates,
        TimeSpan lifetime, EntityUid? targetEntity)
    {
        var ping = Spawn(pingEntityId, coordinates);
        var pingComp = EnsureComp<XenoPingEntityComponent>(ping);

        pingComp.PingType = pingEntityId;
        pingComp.Creator = creator;
        pingComp.Lifetime = lifetime;
        pingComp.DeleteAt = _timing.CurTime + lifetime;
        pingComp.AttachedTarget = targetEntity;

        if (targetEntity != null)
        {
            pingComp.LastKnownCoordinates = coordinates;
            pingComp.WorldPosition = _transform.GetWorldPosition(targetEntity.Value);
        }
        else
        {
            pingComp.WorldPosition = _transform.ToMapCoordinates(coordinates).Position;
        }

        Dirty(ping, pingComp);
        return ping;
    }

    private string GetLocationName(EntityCoordinates coordinates)
    {
        return _areas.TryGetArea(coordinates, out _, out var areaProto) ? areaProto.Name : "Unknown Area";
    }

    private string GetTargetMessage(EntityUid? targetEntity)
    {
        if (targetEntity != null && Exists(targetEntity.Value))
        {
            return $" on {Name(targetEntity.Value)}";
        }
        return string.Empty;
    }

    private void SendHivemindMessage(EntityUid creator, XenoPingDataComponent pingData, string locationName,
        string targetMessage, string creatorName, Color? chatColor, Entity<HiveComponent> hive)
    {
        var hivemindMessage = $";{pingData.ChatMessage} {locationName}{targetMessage}";

        if (_chatSystem.TryProccessRadioMessage(creator, hivemindMessage, out var processedMessage, out _))
        {
            RaiseLocalEvent(creator, new TransformSpeakerNameEvent(creator, creatorName));
            var filter = Filter.Empty().AddWhereAttachedEntity(e => _hive.IsMember(e, hive.Owner));
            _cmChat.ChatMessageToMany(processedMessage, processedMessage, filter, ChatChannel.Radio, creator, colorOverride: chatColor);
        }
    }

    private void ShowPopupToHiveMembers(EntityUid creator, XenoPingDataComponent pingData, string targetMessage,
        EntityCoordinates coordinates, Entity<HiveComponent> hive, PopupType popupType)
    {
        var creatorName = Name(creator);
        var message = $"{creatorName}: {pingData.PopupMessage}{targetMessage}";

        var xenoEnum = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
        while (xenoEnum.MoveNext(out var xenoUid, out _, out var member))
        {
            if (member.Hive == hive.Owner)
            {
                _popup.PopupCoordinates(message, coordinates, xenoUid, popupType);
            }
        }
    }

    private void PlayPingSound(XenoPingDataComponent pingData, EntityUid pingEntity, Entity<HiveComponent> hive)
    {
        if (pingData.Sound == null)
            return;

        var coordinates = Transform(pingEntity).Coordinates;
        var filter = Filter.Empty().AddWhereAttachedEntity(e => _hive.IsMember(e, hive.Owner));
        _audio.PlayStatic(pingData.Sound, filter, coordinates, true, AudioParams.Default.WithVolume(-2f));
    }

    public Dictionary<string, (string Name, string Description)> GetAvailablePingTypes()
    {
        var result = new Dictionary<string, (string Name, string Description)>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            if (!pingData.IsConstruction)
            {
                result[prototype.ID] = (pingData.Name, pingData.Description);
            }
        }

        return result
            .OrderByDescending(x => GetPingDataFromEntityId(x.Key)?.Priority ?? 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public Dictionary<string, (string Name, string Description)> GetAvailableConstructionPingTypes()
    {
        var result = new Dictionary<string, (string Name, string Description)>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            if (pingData.IsConstruction)
            {
                result[prototype.ID] = (pingData.Name, pingData.Description);
            }
        }

        return result
            .OrderByDescending(x => GetPingDataFromEntityId(x.Key)?.Priority ?? 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public Dictionary<string, (string Name, string Description)> GetPingsByCategory(string category)
    {
        var result = new Dictionary<string, (string Name, string Description)>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            if (pingData.Categories.Contains(category))
            {
                result[prototype.ID] = (pingData.Name, pingData.Description);
            }
        }

        return result
            .OrderByDescending(x => GetPingDataFromEntityId(x.Key)?.Priority ?? 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public IEnumerable<string> GetAvailableCategories()
    {
        var categories = new HashSet<string>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            foreach (var category in pingData.Categories)
            {
                categories.Add(category);
            }
        }

        return categories.OrderBy(x => x);
    }

    private XenoPingDataComponent? GetPingDataFromEntityId(string entityId)
    {
        if (!_prototypeManager.TryIndex<EntityPrototype>(entityId, out var prototype))
            return null;

        if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
            return null;

        return (XenoPingDataComponent)pingDataComponent.Component;
    }

    private enum CreatorRole
    {
        Normal,
        HiveLeader,
        Queen
    }
}
