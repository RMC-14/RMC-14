using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[Serializable, NetSerializable]
public sealed partial class XenoTransferPlasmaDoAfterEvent : DoAfterEvent
{
    [DataField]
    public FixedPoint2 Amount = FixedPoint2.New(50);

    public XenoTransferPlasmaDoAfterEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
