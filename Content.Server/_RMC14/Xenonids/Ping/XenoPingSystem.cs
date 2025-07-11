using System.Numerics;
using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Ping;

public sealed class XenoPingSystem : SharedXenoPingSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoPingComponent, XenoPingRequestEvent>(OnPingRequest);
        SubscribeNetworkEvent<XenoPingRequestEvent>(OnNetworkPingRequest);
    }

    private void OnNetworkPingRequest(XenoPingRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var coordinates = GetCoordinates(msg.Coordinates);
        if (!IsValidCoordinates(coordinates))
            return;

        var targetEntity = ResolveTargetEntity(msg.TargetEntity);
        CreatePing(player, msg.PingType, coordinates, targetEntity);
    }

    private EntityUid? ResolveTargetEntity(NetEntity? targetNetEntity)
    {
        if (!targetNetEntity.HasValue)
            return null;

        return TryGetEntity(targetNetEntity.Value, out var entityUid) ? entityUid : null;
    }
}
