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
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Ping;

public abstract class SharedXenoPingSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedCMChatSystem _cmChat = default!;
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

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

    private static readonly Dictionary<string, string> PingTypeToEntity = new()
    {
        ["XenoPingMove"] = "XenoPingMove",
        ["XenoPingDefend"] = "XenoPingDefend",
        ["XenoPingAttack"] = "XenoPingAttack",
        ["XenoPingRegroup"] = "XenoPingRegroup",
        ["XenoPingDanger"] = "XenoPingDanger",
        ["XenoPingHold"] = "XenoPingHold",
        ["XenoPingAmbush"] = "XenoPingAmbush",
        ["XenoPingFortify"] = "XenoPingFortify",
        ["XenoPingWeed"] = "XenoPingWeed",
        ["XenoPingNest"] = "XenoPingNest",
        ["XenoPingHosts"] = "XenoPingHosts",
        ["XenoPingAide"] = "XenoPingAide",
        ["XenoPingGeneral"] = "XenoPingGeneral"
    };

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

    private EntityUid? ResolveTargetEntity(NetEntity? targetNetEntity)
    {
        if (!targetNetEntity.HasValue)
            return null;

        return TryGetEntity(targetNetEntity.Value, out var entityUid) ? entityUid : null;
    }

    protected void CreatePing(EntityUid creator, string pingType, EntityCoordinates coordinates, EntityUid? targetEntity = null)
    {
        if (_net.IsClient)
            return;

        if (!ValidateXenoCreator(creator, out var hive))
            return;

        var creatorRole = DetermineCreatorRole(creator);
        EnforcePingLimits(creator, creatorRole);
        CreatePingBasedOnRole(creator, pingType, coordinates, hive, targetEntity, creatorRole);
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

    private void CreatePingBasedOnRole(EntityUid creator, string pingType, EntityCoordinates coordinates,
        Entity<HiveComponent> hive, EntityUid? targetEntity, CreatorRole role)
    {
        var (lifetime, color, popupType) = role switch
        {
            CreatorRole.Queen => (TimeSpan.FromSeconds(QueenPingLifetimeSeconds), Color.FromHex("#FFD700"), PopupType.Large),
            CreatorRole.HiveLeader => (TimeSpan.FromSeconds(HiveLeaderPingLifetimeSeconds), Color.FromHex("#FF4500"), PopupType.Large),
            _ => (TimeSpan.FromSeconds(NormalPingLifetimeSeconds), (Color?)null, PopupType.Medium)
        };

        CreatePingWithRole(creator, pingType, coordinates, hive, lifetime, color, popupType, targetEntity);
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

    private void CreatePingWithRole(EntityUid creator, string pingType, EntityCoordinates coordinates,
        Entity<HiveComponent> hive, TimeSpan lifetime, Color? chatColor, PopupType popupType, EntityUid? targetEntity = null)
    {
        var ping = CreatePingEntity(creator, pingType, coordinates, lifetime, targetEntity);
        var locationName = GetLocationName(coordinates);
        var creatorName = Name(creator);
        var targetMessage = GetTargetMessage(targetEntity);

        SendHivemindMessage(creator, pingType, locationName, targetMessage, creatorName, chatColor, hive);
        ShowPopupToHiveMembers(creator, pingType, targetMessage, coordinates, hive, popupType);
    }

    private EntityUid CreatePingEntity(EntityUid creator, string pingType, EntityCoordinates coordinates,
        TimeSpan lifetime, EntityUid? targetEntity)
    {
        var ping = Spawn(GetPingEntityId(pingType), coordinates);
        var pingComp = EnsureComp<XenoPingEntityComponent>(ping);

        pingComp.PingType = pingType;
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

    private void SendHivemindMessage(EntityUid creator, string pingType, string locationName,
        string targetMessage, string creatorName, Color? chatColor, Entity<HiveComponent> hive)
    {
        var hivemindMessage = $";{GetPingMessage(pingType)} {locationName}{targetMessage}";

        if (_chatSystem.TryProccessRadioMessage(creator, hivemindMessage, out var processedMessage, out _))
        {
            RaiseLocalEvent(creator, new TransformSpeakerNameEvent(creator, creatorName));
            var filter = Filter.Empty().AddWhereAttachedEntity(e => _hive.IsMember(e, hive.Owner));
            _cmChat.ChatMessageToMany(processedMessage, processedMessage, filter, ChatChannel.Radio, creator, colorOverride: chatColor);
        }
    }

    private void ShowPopupToHiveMembers(EntityUid creator, string pingType, string targetMessage,
        EntityCoordinates coordinates, Entity<HiveComponent> hive, PopupType popupType)
    {
        var creatorName = Name(creator);
        var message = $"{creatorName}: {GetPingName(pingType)}{targetMessage}";

        var xenoEnum = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
        while (xenoEnum.MoveNext(out var xenoUid, out _, out var member))
        {
            if (member.Hive == hive.Owner)
            {
                _popup.PopupCoordinates(message, coordinates, xenoUid, popupType);
            }
        }
    }

    protected string GetPingEntityId(string pingType)
    {
        return PingTypeToEntity.GetValueOrDefault(pingType, "XenoPingGeneral");
    }

    protected string GetPingMessage(string pingType)
    {
        return pingType switch
        {
            "XenoPingMove" => "Move to",
            "XenoPingDefend" => "Defend",
            "XenoPingAttack" => "Attack",
            "XenoPingRegroup" => "Regroup at",
            "XenoPingDanger" => "Danger at",
            "XenoPingHold" => "Hold",
            "XenoPingAmbush" => "Ambush at",
            "XenoPingFortify" => "Fortify",
            "XenoPingWeed" => "Weed",
            "XenoPingNest" => "Nest at",
            "XenoPingHosts" => "Hosts at",
            "XenoPingAide" => "Help at",
            "XenoPingGeneral" => "Look at",
            _ => "Look at"
        };
    }

    protected string GetPingName(string pingType)
    {
        return pingType switch
        {
            "XenoPingMove" => "Move Here!",
            "XenoPingDefend" => "Defend This Position!",
            "XenoPingAttack" => "Attack Here!",
            "XenoPingRegroup" => "Regroup Here!",
            "XenoPingDanger" => "Danger!",
            "XenoPingHold" => "Hold Position!",
            "XenoPingAmbush" => "Ambush Here!",
            "XenoPingFortify" => "Fortify This Area!",
            "XenoPingWeed" => "Spread Weeds Here!",
            "XenoPingNest" => "Build Nest Here!",
            "XenoPingHosts" => "Hosts Detected!",
            "XenoPingAide" => "Need Assistance!",
            "XenoPingGeneral" => "Follow Orders!",
            _ => "Follow Orders!"
        };
    }

    public static Dictionary<string, (string Name, string Description)> GetAvailablePingTypes()
    {
        return new Dictionary<string, (string Name, string Description)>
        {
            ["XenoPingMove"] = ("Move", "Direct sisters to move to this location"),
            ["XenoPingDefend"] = ("Defend", "Mark this position for defense"),
            ["XenoPingAttack"] = ("Attack", "Target this location for attack"),
            ["XenoPingRegroup"] = ("Regroup", "Rally sisters at this position"),
            ["XenoPingDanger"] = ("Danger", "Warning! Dangerous area"),
            ["XenoPingHold"] = ("Hold", "Hold this position"),
            ["XenoPingAmbush"] = ("Ambush", "Set up ambush here"),
            ["XenoPingFortify"] = ("Fortify", "Build defenses here"),
            ["XenoPingWeed"] = ("Weed", "Spread weeds in this area"),
            ["XenoPingNest"] = ("Nest", "Build nest structures here"),
            ["XenoPingHosts"] = ("Hosts", "Potential hosts detected"),
            ["XenoPingAide"] = ("Aide", "Need assistance here"),
            ["XenoPingGeneral"] = ("General", "General purpose marker")
        };
    }


    // WIP
    public static Dictionary<string, (string Name, string Description)> GetAvailableConstructionPingTypes()
    {
        return new Dictionary<string, (string Name, string Description)>
        {
            // ["XenoPingHiveCore"] = ("Hive Core", "Main hive structure location"),
            // ["XenoPingHiveCluster"] = ("Hive Cluster", "Hive cluster that spreads weeds"),
            // ["XenoPingHivePylon"] = ("Hive Pylon", "Hive pylon structure"),
            // ["XenoPingEggMorpher"] = ("Egg Morpher", "Egg morpher structure location"),
            // ["XenoPingRecoveryNode"] = ("Recovery Node", "Recovery node for healing"),
            // ["XenoPingTunnel"] = ("Tunnel", "Tunnel entrance location"),
            // ["XenoPingResinWall"] = ("Resin Wall", "Resin wall construction"),
            // ["XenoPingResinDoor"] = ("Resin Door", "Resin door construction"),
            // ["XenoPingResinHole"] = ("Resin Hole", "Resin trap hole location"),
            // ["XenoPingFruit"] = ("Fruit", "Resin fruit location")
        };
    }

    private enum CreatorRole
    {
        Normal,
        HiveLeader,
        Queen
    }
}
