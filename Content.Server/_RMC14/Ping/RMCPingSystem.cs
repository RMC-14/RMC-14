using Content.Shared._RMC14.Ping;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._RMC14.Ping;

public abstract class RMCPingSystem<TPingComponent, TPingEntityComponent, TPingDataComponent, TPingRequestEvent>
    : SharedRMCPingSystem<TPingEntityComponent, TPingDataComponent>
    where TPingComponent : Component
    where TPingEntityComponent : Component, RMCPingEntityComponent, new()
    where TPingDataComponent : Component, RMCPingDataComponent
    where TPingRequestEvent : RMCPingRequestEvent
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _lastPingTimes = new();

    protected virtual double MinPingDelaySeconds => 2.0;
    protected virtual string CooldownPopupMessage => "You must wait before pinging again.";
    protected virtual string InvalidPingTypePopupMessage => "Invalid ping type.";
    protected virtual string InvalidPingConfigPopupMessage => "Invalid ping configuration.";

    protected SharedPopupSystem Popup => _popup;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TPingComponent, TPingRequestEvent>(OnPingRequest);
        SubscribeNetworkEvent<TPingRequestEvent>(OnNetworkPingRequest);
    }

    private void OnPingRequest(Entity<TPingComponent> ent, ref TPingRequestEvent args)
    {
        HandlePingRequest(ent.Owner, args);
    }

    private void OnNetworkPingRequest(TPingRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        HandlePingRequest(player, msg);
    }

    private void HandlePingRequest(EntityUid creator, TPingRequestEvent msg)
    {
        if (!ValidatePingCooldown(creator))
            return;

        var coordinates = GetCoordinates(msg.Coordinates);
        if (!IsValidCoordinates(coordinates))
            return;

        if (!_prototypeManager.TryIndex<EntityPrototype>(msg.PingType, out var entityProto))
        {
            _popup.PopupEntity(InvalidPingTypePopupMessage, creator, creator, PopupType.SmallCaution);
            return;
        }

        if (!TryGetPingData(entityProto, out var pingData))
        {
            _popup.PopupEntity(InvalidPingConfigPopupMessage, creator, creator, PopupType.SmallCaution);
            return;
        }

        var targetEntity = ResolveTargetEntity(msg.TargetEntity);
        OnPingRequestValidated(creator, msg.PingType, pingData, coordinates, targetEntity);
    }

    protected abstract void OnPingRequestValidated(
        EntityUid creator,
        string pingEntityId,
        TPingDataComponent pingData,
        EntityCoordinates coordinates,
        EntityUid? targetEntity);

    protected EntityUid SpawnPingEntity(
        EntityUid creator,
        string pingEntityId,
        EntityCoordinates coordinates,
        TimeSpan lifetime,
        EntityUid? targetEntity = null)
    {
        var ping = Spawn(pingEntityId, coordinates);
        var pingComp = EnsureComp<TPingEntityComponent>(ping);

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

    private bool ValidatePingCooldown(EntityUid creator)
    {
        var currentTime = _timing.CurTime;

        if (_lastPingTimes.TryGetValue(creator, out var lastPing))
        {
            var timeSinceLastPing = (currentTime - lastPing).TotalSeconds;
            if (timeSinceLastPing < MinPingDelaySeconds)
            {
                _popup.PopupEntity(CooldownPopupMessage, creator, creator, PopupType.SmallCaution);
                return false;
            }
        }

        _lastPingTimes[creator] = currentTime;
        return true;
    }

    private static bool TryGetPingData(
        EntityPrototype prototype,
        [NotNullWhen(true)] out TPingDataComponent? pingData)
    {
        foreach (var component in prototype.Components.Values)
        {
            if (component.Component is TPingDataComponent typedData)
            {
                pingData = typedData;
                return true;
            }
        }

        pingData = null;
        return false;
    }
}
