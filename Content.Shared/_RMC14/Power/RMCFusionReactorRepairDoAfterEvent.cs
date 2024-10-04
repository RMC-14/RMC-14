using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[Serializable, NetSerializable]
public sealed partial class RMCFusionReactorRepairDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public RMCFusionReactorState State;

    public RMCFusionReactorRepairDoAfterEvent(RMCFusionReactorState state)
    {
        State = state;
    }
}
