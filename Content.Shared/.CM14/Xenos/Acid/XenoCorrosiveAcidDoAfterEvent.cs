using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CM14.Xenos.Acid;

[Serializable, NetSerializable]
public sealed partial class XenoCorrosiveAcidDoAfterEvent : DoAfterEvent
{
    [DataField]
    public EntProtoId AcidId;

    [DataField]
    public int PlasmaCost;

    [DataField]
    public TimeSpan Time;

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
