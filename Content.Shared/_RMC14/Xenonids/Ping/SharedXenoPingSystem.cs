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

    private readonly Dictionary<EntityUid, TimeSpan> _lastPingTimes = new();
    private const double MinPingDelay = 2.0;
    private const int NormalXenoPingLimit = 3;
    private const int HiveLeaderPingLimit = 8;

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
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var currentTime = _timing.CurTime;
        var toDelete = new List<EntityUid>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (ping.AttachedTarget.HasValue)
            {
                if (Exists(ping.AttachedTarget.Value))
                {
                    var targetXform = Transform(ping.AttachedTarget.Value);
                    var targetCoordinates = targetXform.Coordinates;
                    var targetWorldPos = _transform.GetWorldPosition(ping.AttachedTarget.Value);
                    var currentWorldPos = _transform.GetWorldPosition(uid);

                    var distanceMoved = Vector2.Distance(targetWorldPos, currentWorldPos);

                    if (distanceMoved > 0.001f)
                    {
                        ping.LastKnownCoordinates = targetCoordinates;
                        ping.WorldPosition = targetWorldPos;
                        _transform.SetCoordinates(uid, targetCoordinates);
                        Dirty(uid, ping);
                    }
                }
                else
                {
                    if (ping.LastKnownCoordinates.HasValue)
                    {
                        _transform.SetCoordinates(uid, ping.LastKnownCoordinates.Value);
                    }

                    ping.AttachedTarget = null;
                    Dirty(uid, ping);
                }
            }

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

        var currentTime = _timing.CurTime;
        if (_lastPingTimes.TryGetValue(ent.Owner, out var lastPing))
        {
            var timeSinceLastPing = (currentTime - lastPing).TotalSeconds;
            if (timeSinceLastPing < MinPingDelay)
            {
                _popup.PopupEntity("You must wait before pinging again.", ent.Owner, ent.Owner, PopupType.SmallCaution);
                return;
            }
        }

        _lastPingTimes[ent.Owner] = currentTime;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!coordinates.IsValid(EntityManager) || !_mapManager.MapExists(coordinates.GetMapId(EntityManager)))
            return;

        EntityUid? targetEntity = null;
        if (args.TargetEntity.HasValue)
        {
            if (TryGetEntity(args.TargetEntity.Value, out var entityUid))
            {
                targetEntity = entityUid;
            }
        }

        CreatePing(ent.Owner, args.PingType, coordinates, targetEntity);
    }

    protected void CreatePing(EntityUid creator, string pingType, EntityCoordinates coordinates, EntityUid? targetEntity = null)
    {
        if (_net.IsClient)
            return;

        var isXeno = HasComp<XenoComponent>(creator);
        if (!isXeno)
            return;

        var hive = _hive.GetHive(creator);
        if (hive == null)
        {
            _popup.PopupEntity("You must be in a hive to ping.", creator, creator, PopupType.SmallCaution);
            return;
        }

        var isQueen = HasComp<XenoWordQueenComponent>(creator);
        var isHiveLeader = _hiveLeader.IsLeader(creator, out _);

        EnforcePingLimits(creator, isQueen, isHiveLeader);

        if (isQueen)
        {
            CreateQueenWaypoint(creator, pingType, coordinates, hive.Value, targetEntity);
        }
        else if (isHiveLeader)
        {
            CreateHiveLeaderWaypoint(creator, pingType, coordinates, hive.Value, targetEntity);
        }
        else
        {
            CreateNormalPing(creator, pingType, coordinates, hive.Value, targetEntity);
        }
    }

    private void EnforcePingLimits(EntityUid creator, bool isQueen, bool isHiveLeader)
    {
        if (isQueen)
            return;

        var maxPings = isHiveLeader ? HiveLeaderPingLimit : NormalXenoPingLimit;

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

        creatorPings.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

        var pingsToRemove = creatorPings.Count - maxPings + 1;
        for (var i = 0; i < pingsToRemove && i < creatorPings.Count; i++)
        {
            QueueDel(creatorPings[i].Uid);
        }
    }

    private void CreateQueenWaypoint(EntityUid creator, string pingType, EntityCoordinates coordinates, Entity<HiveComponent> hive, EntityUid? targetEntity = null)
    {
        CreatePingWithRole(creator, pingType, coordinates, hive, TimeSpan.FromSeconds(40), Color.FromHex("#FFD700"), PopupType.Large, targetEntity);
    }

    private void CreateHiveLeaderWaypoint(EntityUid creator, string pingType, EntityCoordinates coordinates, Entity<HiveComponent> hive, EntityUid? targetEntity = null)
    {
        CreatePingWithRole(creator, pingType, coordinates, hive, TimeSpan.FromSeconds(25), Color.FromHex("#FF4500"), PopupType.Large, targetEntity);
    }

    private void CreateNormalPing(EntityUid creator, string pingType, EntityCoordinates coordinates, Entity<HiveComponent> hive, EntityUid? targetEntity = null)
    {
        if (_net.IsClient) return;
        CreatePingWithRole(creator, pingType, coordinates, hive, TimeSpan.FromSeconds(8), null, PopupType.Medium, targetEntity);
    }

    private void CreatePingWithRole(EntityUid creator, string pingType, EntityCoordinates coordinates, Entity<HiveComponent> hive, TimeSpan lifetime, Color? chatColor, PopupType popupType, EntityUid? targetEntity = null)
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

        var locationName = _areas.TryGetArea(coordinates, out _, out var areaProto) ? areaProto.Name : "Unknown Area";
        var creatorName = Name(creator);

        var targetMessage = "";
        if (targetEntity != null && Exists(targetEntity.Value))
        {
            var targetName = Name(targetEntity.Value);
            targetMessage = $" on {targetName}";
        }

        var hivemindMessage = $";{GetPingMessage(pingType)} {locationName}{targetMessage}";

        if (_chatSystem.TryProccessRadioMessage(creator, hivemindMessage, out var processedMessage, out _))
        {
            RaiseLocalEvent(creator, new TransformSpeakerNameEvent(creator, creatorName));
            var filter = Filter.Empty().AddWhereAttachedEntity(e => _hive.IsMember(e, hive.Owner));
            _cmChat.ChatMessageToMany(processedMessage, processedMessage, filter, ChatChannel.Radio, creator, colorOverride: chatColor);
        }

        var xenoQuery = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
        while (xenoQuery.MoveNext(out var xenoUid, out _, out var member))
        {
            if (member.Hive == hive.Owner)
                _popup.PopupCoordinates($"{creatorName}: {GetPingName(pingType)}{targetMessage}", coordinates, xenoUid, popupType);
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
}
