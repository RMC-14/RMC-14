using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.Surgery;

[Serializable, NetSerializable]
public sealed partial class CMSurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly NetEntity Part;
    public readonly EntProtoId Surgery;
    public readonly EntProtoId Step;

    public CMSurgeryDoAfterEvent(NetEntity part, EntProtoId surgery, EntProtoId step)
    {
        Part = part;
        Surgery = surgery;
        Step = step;
    }
}
