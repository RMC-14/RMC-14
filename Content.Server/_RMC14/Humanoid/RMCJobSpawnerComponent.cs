using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Humanoid;

[RegisterComponent]
[Access(typeof(RMCHumanoidSystem))]
public sealed partial class RMCJobSpawnerComponent : Component
{
    [DataField]
    public ProtoId<JobPrototype>? Job;

    [DataField]
    public bool Loadout = true;
}
