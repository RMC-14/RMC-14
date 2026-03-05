using Content.Shared._RMC14.Ping;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Ping;

[Serializable, NetSerializable]
public sealed class XenoPingRequestEvent : RMCPingRequestEvent
{
    public XenoPingRequestEvent(string pingType, NetCoordinates coordinates, NetEntity? targetEntity = null)
        : base(pingType, coordinates, targetEntity)
    {
    }
}
