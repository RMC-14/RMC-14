using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.FaxResponder;

[RegisterComponent, NetworkedComponent]
public sealed partial class FaxResponderSpawnPointComponent : Component
{
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;
}
