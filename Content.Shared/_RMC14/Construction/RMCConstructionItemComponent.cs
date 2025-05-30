using Content.Shared._RMC14.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCConstructionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<RMCConstructionPrototype>[]? Buildable;
}
