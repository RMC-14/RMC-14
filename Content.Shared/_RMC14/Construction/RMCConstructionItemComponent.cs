using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCConstructionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>[]? Buildable;
}
