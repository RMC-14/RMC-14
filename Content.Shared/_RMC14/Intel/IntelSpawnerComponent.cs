using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, EntityCategory("Spawner")]
[Access(typeof(IntelSystem))]
public sealed partial class IntelSpawnerComponent : Component
{
    [DataField, AutoNetworkedField]
    public IntelSpawnerType IntelType;
}
