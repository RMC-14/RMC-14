using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCProduceComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? PlantPrototype;

    [DataField, AutoNetworkedField]
    public string Name = "";

    [DataField, AutoNetworkedField]
    public string Noun = "";

    [DataField]
    public string SolutionName = "food";
}
