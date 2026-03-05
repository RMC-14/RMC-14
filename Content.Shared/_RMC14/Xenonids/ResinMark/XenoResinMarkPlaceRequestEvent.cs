using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ResinMark;

[Serializable, NetSerializable]
public sealed class XenoResinMarkPlaceRequestEvent : EntityEventArgs
{
    public NetCoordinates Coordinates { get; }

    public XenoResinMarkPlaceRequestEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}

