using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.Aid;

public sealed partial class XenoAidActionEvent : EntityTargetActionEvent
{
    [DataField]
    public XenoAidMode aidType = XenoAidMode.Healing;
}
