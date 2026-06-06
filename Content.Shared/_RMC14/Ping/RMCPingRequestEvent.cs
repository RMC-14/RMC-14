using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ping;

[Serializable, NetSerializable]
public abstract class RMCPingRequestEvent : EntityEventArgs
{
    public string PingType { get; }
    public NetCoordinates Coordinates { get; }
    public NetEntity? TargetEntity { get; }

    protected RMCPingRequestEvent(string pingType, NetCoordinates coordinates, NetEntity? targetEntity = null)
    {
        PingType = pingType;
        Coordinates = coordinates;
        TargetEntity = targetEntity;
    }
}
