using System.Numerics;
using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Ping;

public sealed class XenoPingSystem : SharedXenoPingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoPingComponent, XenoPingRequestEvent>(OnPingRequest);
        SubscribeNetworkEvent<XenoPingRequestEvent>(OnNetworkPingRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateAttachedTargetPositions();
    }

    private void UpdateAttachedTargetPositions()
    {
        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (!ping.AttachedTarget.HasValue)
                continue;

            var targetEntity = ping.AttachedTarget.Value;

            if (!Exists(targetEntity))
            {
                ping.AttachedTarget = null;
                Dirty(uid, ping);
                continue;
            }

            if (!TryComp<TransformComponent>(targetEntity, out var targetTransform))
                continue;

            var targetCoordinates = targetTransform.Coordinates;
            var targetWorldPos = _transform.GetWorldPosition(targetEntity);
            var currentPingWorldPos = _transform.GetWorldPosition(uid);

            var distanceMoved = Vector2.Distance(targetWorldPos, currentPingWorldPos);

            if (distanceMoved > 0.01f)
            {
                _transform.SetCoordinates(uid, targetCoordinates);
                ping.LastKnownCoordinates = targetCoordinates;
                ping.WorldPosition = targetWorldPos;
                Dirty(uid, ping);
            }
        }
    }

    private void OnNetworkPingRequest(XenoPingRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
        {
            return;
        }

        var coordinates = GetCoordinates(msg.Coordinates);

        EntityUid? targetEntity = null;
        if (msg.TargetEntity.HasValue)
        {
            if (TryGetEntity(msg.TargetEntity.Value, out var entityUid))
            {
                targetEntity = entityUid;
            }
        }

        CreatePing(player, msg.PingType, coordinates, targetEntity);
    }
}
