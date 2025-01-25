using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.Cleave;

public sealed partial class XenoCleaveActionEvent : EntityTargetActionEvent
{
    [DataField]
    public bool Flings = false;
}
