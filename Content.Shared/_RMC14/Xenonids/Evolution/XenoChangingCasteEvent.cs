using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[Serializable, NetSerializable]
public sealed class XenoChangingCasteEvent : EntityEventArgs
{
    public NetEntity Xeno;
    public EntProtoId NewProtoId;

    public XenoChangingCasteEvent(NetEntity xeno, EntProtoId newProtoId)
    {
        Xeno = xeno;
        NewProtoId = newProtoId;
    }
}
