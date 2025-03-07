using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Food;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCLabelByContainedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype> TagToCheck = "MREMain";
}
