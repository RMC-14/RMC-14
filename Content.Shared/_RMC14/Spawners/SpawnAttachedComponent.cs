using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Spawners;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class SpawnAttachedComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Spawn;
}
