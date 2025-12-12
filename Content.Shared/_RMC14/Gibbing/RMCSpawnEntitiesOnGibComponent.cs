using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Gibbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSpawnEntitiesOnGibComponent : Component
{
    /// <summary>
    /// Entities to spawn when this entity gibs
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Entities = new();
}
