using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Surgery;

[Serializable, NetSerializable]
public sealed partial class CMSurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly EntProtoId Surgery;
    public readonly EntProtoId Step;
    public readonly float SuccessChance;

    public CMSurgeryDoAfterEvent(EntProtoId surgery, EntProtoId step, float successChance)
    {
        Surgery = surgery;
        Step = step;
        SuccessChance = successChance;
    }
}
