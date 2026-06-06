using Content.Server._RMC14.Ping;
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
using System;
using System.Collections.Generic;

namespace Content.Server._RMC14.Xenonids.Ping;

public sealed class XenoPingSystem : RMCPingSystem<XenoPingComponent, XenoPingEntityComponent, XenoPingDataComponent, XenoPingRequestEvent>
{
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedCMChatSystem _cmChat = default!;
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly HiveLeaderSystem _hiveLeader = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const int NormalXenoPingLimit = 3;
    private const int HiveLeaderPingLimit = 8;
    private const double QueenPingLifetimeSeconds = 40.0;
    private const double HiveLeaderPingLifetimeSeconds = 25.0;
    private const double NormalPingLifetimeSeconds = 8.0;

    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        base.Initialize();

        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoPingEntityComponent, ComponentShutdown>(OnPingEntityShutdown);
    }

    protected override void OnPingRequestValidated(EntityUid creator, string pingEntityId, XenoPingDataComponent pingData,
        EntityCoordinates coordinates, EntityUid? targetEntity)
    {
        if (!ValidateXenoCreator(creator, out var hive))
            return;

        targetEntity = ValidateTargetEntity(creator, targetEntity);

        var creatorRole = DetermineCreatorRole(creator);

        EnforcePingLimits(creator, creatorRole);
        var createdPingUid = CreatePingBasedOnRole(creator, pingEntityId, pingData, coordinates, hive, targetEntity, creatorRole);

        if (createdPingUid != EntityUid.Invalid)
        {
            AddPingToPvsOverrides(createdPingUid);
        }
    }

    private EntityUid? ValidateTargetEntity(EntityUid creator, EntityUid? targetEntity)
    {
        if (targetEntity == null)
            return null;

        if (!_xenoQuery.HasComponent(targetEntity.Value))
            return null;

        return _hive.FromSameHive(creator, targetEntity.Value) ? targetEntity : null;
    }

    private bool ValidateXenoCreator(EntityUid creator, out Entity<HiveComponent> hive)
    {
        hive = default;

        if (!_xenoQuery.HasComponent(creator))
            return false;

        var hiveEntity = _hive.GetHive(creator);
        if (hiveEntity == null)
        {
            Popup.PopupEntity("You must be in a hive to ping.", creator, creator, PopupType.SmallCaution);
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
        var (lifetime, roleColor, popupType) = GetRolePingSettings(role);

        return CreatePingWithRole(creator, pingEntityId, pingData, coordinates, hive, lifetime, roleColor, popupType, targetEntity);
    }

    public void SendRoleBasedPingCallout(
        EntityUid creator,
        EntityUid pingEntity,
        XenoPingDataComponent pingData,
        EntityCoordinates coordinates,
        Entity<HiveComponent> hive,
        EntityUid? targetEntity = null)
    {
        var (_, roleColor, popupType) = GetRolePingSettings(DetermineCreatorRole(creator));
        SendPingCallout(creator, pingEntity, pingData, coordinates, hive, roleColor, popupType, targetEntity);
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
        var ping = SpawnPingEntity(creator, pingEntityId, coordinates, lifetime, targetEntity);
        if (TryComp<XenoPingEntityComponent>(ping, out var pingComp))
        {
            pingComp.Hive = hive.Owner;
            Dirty(ping, pingComp);
        }

        SendPingCallout(creator, ping, pingData, coordinates, hive, chatColor, popupType, targetEntity);

        return ping;
    }

    private void SendPingCallout(
        EntityUid creator,
        EntityUid pingEntity,
        XenoPingDataComponent pingData,
        EntityCoordinates coordinates,
        Entity<HiveComponent> hive,
        Color? chatColor,
        PopupType popupType,
        EntityUid? targetEntity)
    {
        var locationName = GetLocationName(coordinates);
        var creatorName = Name(creator);
        var targetMessage = GetTargetMessage(targetEntity);

        SendHivemindMessage(creator, pingData, locationName, targetMessage, creatorName, chatColor, hive);
        ShowPopupToHiveMembers(creator, pingData, targetMessage, coordinates, hive, popupType);
        PlayPingSound(pingData, pingEntity, hive);
    }

    private static (TimeSpan Lifetime, Color? ChatColor, PopupType PopupType) GetRolePingSettings(CreatorRole role)
    {
        return role switch
        {
            CreatorRole.Queen => (TimeSpan.FromSeconds(QueenPingLifetimeSeconds), Color.FromHex("#D8B4FF"), PopupType.Large),
            CreatorRole.HiveLeader => (TimeSpan.FromSeconds(HiveLeaderPingLifetimeSeconds), Color.FromHex("#FF4500"), PopupType.Large),
            _ => (TimeSpan.FromSeconds(NormalPingLifetimeSeconds), null, PopupType.Medium)
        };
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
                Popup.PopupCoordinates(message, coordinates, xenoUid, popupType);
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
