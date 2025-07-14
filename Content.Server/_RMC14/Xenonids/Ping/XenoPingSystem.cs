using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.Ping;
using Content.Shared._RMC14.Xenonids.Word;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Server._RMC14.Xenonids.Ping;

public sealed class XenoPingSystem : SharedXenoPingSystem
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedCMChatSystem _cmChat = default!;
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const double MinPingDelaySeconds = 2.0;
    private const int NormalXenoPingLimit = 3;
    private const int HiveLeaderPingLimit = 8;
    private const double QueenPingLifetimeSeconds = 40.0;
    private const double HiveLeaderPingLifetimeSeconds = 25.0;
    private const double NormalPingLifetimeSeconds = 8.0;

    private EntityQuery<XenoComponent> _xenoQuery;
    private readonly Dictionary<EntityUid, TimeSpan> _lastPingTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoPingComponent, XenoPingRequestEvent>(OnPingRequest);
        SubscribeNetworkEvent<XenoPingRequestEvent>(OnNetworkPingRequest);
        SubscribeLocalEvent<XenoPingEntityComponent, ComponentShutdown>(OnPingEntityShutdown);
    }

    private void OnPingRequest(Entity<XenoPingComponent> ent, ref XenoPingRequestEvent args)
    {
        if (!ValidatePingCooldown(ent.Owner))
            return;

        var coordinates = GetCoordinates(args.Coordinates);
        if (!IsValidCoordinates(coordinates))
            return;

        var targetEntity = ResolveTargetEntity(args.TargetEntity);
        CreatePing(ent.Owner, args.PingType, coordinates, targetEntity);
    }

    private void OnNetworkPingRequest(XenoPingRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!ValidatePingCooldown(player))
            return;

        var coordinates = GetCoordinates(msg.Coordinates);
        if (!IsValidCoordinates(coordinates))
            return;

        var targetEntity = ResolveTargetEntity(msg.TargetEntity);
        CreatePing(player, msg.PingType, coordinates, targetEntity);
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

    private void CreatePing(EntityUid creator, string pingEntityId, EntityCoordinates coordinates, EntityUid? targetEntity = null)
    {
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
        var createdPingUid = CreatePingBasedOnRole(creator, pingEntityId, pingData, coordinates, hive, targetEntity, creatorRole);

        if (createdPingUid != EntityUid.Invalid)
        {
            AddPingToPvsOverrides(createdPingUid);
        }
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

    private EntityUid CreatePingBasedOnRole(EntityUid creator, string pingEntityId, XenoPingDataComponent pingData,
        EntityCoordinates coordinates, Entity<HiveComponent> hive, EntityUid? targetEntity, CreatorRole role)
    {
        var (lifetime, roleColor, popupType) = role switch
        {
            CreatorRole.Queen => (TimeSpan.FromSeconds(QueenPingLifetimeSeconds), Color.FromHex("#FFD700"), PopupType.Large),
            CreatorRole.HiveLeader => (TimeSpan.FromSeconds(HiveLeaderPingLifetimeSeconds), Color.FromHex("#FF4500"), PopupType.Large),
            _ => (TimeSpan.FromSeconds(NormalPingLifetimeSeconds), (Color?)null, PopupType.Medium)
        };

        return CreatePingWithRole(creator, pingEntityId, pingData, coordinates, hive, lifetime, roleColor, popupType, targetEntity);
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

    private EntityUid CreatePingWithRole(EntityUid creator, string pingEntityId, XenoPingDataComponent pingData,
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

        return ping;
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

    private void OnPingEntityShutdown(Entity<XenoPingEntityComponent> ent, ref ComponentShutdown args)
    {
        RemovePingFromPvsOverrides(ent.Owner);
    }

    private void AddPingToPvsOverrides(EntityUid pingUid)
    {
        if (!TryComp<XenoPingEntityComponent>(pingUid, out var pingComp))
            return;

        var creatorHive = _hive.GetHive(pingComp.Creator);
        if (creatorHive == null)
            return;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } playerEntity)
                continue;

            if (!HasComp<HiveMemberComponent>(playerEntity))
                continue;

            var playerHive = _hive.GetHive(playerEntity);
            if (playerHive == null || playerHive.Value.Owner != creatorHive.Value.Owner)
                continue;

            _pvsOverride.AddSessionOverride(pingUid, session);
        }
    }

    private void RemovePingFromPvsOverrides(EntityUid pingUid)
    {
        foreach (var session in _playerManager.Sessions)
        {
            _pvsOverride.RemoveSessionOverride(pingUid, session);
        }
    }

    private enum CreatorRole
    {
        Normal,
        HiveLeader,
        Queen
    }
}
