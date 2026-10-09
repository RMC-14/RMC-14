using Content.Shared.Actions;

namespace Content.Shared._RMC14.Marines.Mutiny;

public sealed partial class MutineerRecruitActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float Range = 1.5f;
}

public sealed partial class MutineerBeginActionEvent : InstantActionEvent;
