using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Stations;

[RegisterComponent]
[Access(typeof(RMCStationJobsSystem))]
public sealed partial class JobSlotScalingComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, JobSlotScaling> Jobs = new();
}
