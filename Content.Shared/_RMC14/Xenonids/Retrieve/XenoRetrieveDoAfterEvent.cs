using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Retrieve;

[Serializable, NetSerializable]
public sealed partial class XenoRetrieveDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetEntity Action;

    public XenoRetrieveDoAfterEvent(NetEntity action)
    {
        Action = action;
    }
}
