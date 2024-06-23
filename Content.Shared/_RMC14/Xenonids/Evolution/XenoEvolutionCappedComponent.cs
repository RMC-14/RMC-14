using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoEvolutionCappedComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Id;

    [DataField(required: true), AutoNetworkedField]
    public int Max;
}
