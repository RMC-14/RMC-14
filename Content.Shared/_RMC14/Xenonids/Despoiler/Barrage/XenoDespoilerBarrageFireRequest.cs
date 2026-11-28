using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Barrage;

[Serializable, NetSerializable]
public sealed class XenoDespoilerBarrageFireRequest(NetCoordinates target) : EntityEventArgs
{
    public NetCoordinates Target = target;
}
