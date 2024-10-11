using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Roles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OriginalRoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Job;
}
