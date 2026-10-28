using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCPlantSeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? PlantPrototype;

    [DataField]
    public float? HealthOverride;
}
