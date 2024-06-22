using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenonids.Acid;

[Serializable, NetSerializable]
public sealed partial class XenoCorrosiveAcidDoAfterEvent : DoAfterEvent
{
    [DataField]
    public EntProtoId AcidId = "XenoAcidNormal";

    [DataField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(150);

    public XenoCorrosiveAcidDoAfterEvent(XenoCorrosiveAcidEvent ev)
    {
        AcidId = ev.AcidId;
        PlasmaCost = ev.PlasmaCost;
        Time = ev.Time;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
