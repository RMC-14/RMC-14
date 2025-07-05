using Content.Shared._RMC14.Xenonids.Ping;

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
        {
            return;
        }

        var coordinates = GetCoordinates(msg.Coordinates);
        CreatePing(player, msg.PingType, coordinates);
    }
}
