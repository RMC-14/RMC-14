using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Ping;

[Serializable, NetSerializable]
public sealed class XenoPingRequestEvent : EntityEventArgs
{
    public string PingType { get; }
    public NetCoordinates Coordinates { get; }
    public NetEntity? TargetEntity { get; }

    public XenoPingRequestEvent(string pingType, NetCoordinates coordinates, NetEntity? targetEntity = null)
    {
        PingType = pingType;
        Coordinates = coordinates;
        TargetEntity = targetEntity;
    }
}
