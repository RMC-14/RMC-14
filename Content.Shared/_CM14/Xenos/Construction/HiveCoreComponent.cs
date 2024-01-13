using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class HiveCoreComponent : Component
{
    // TODO CM14 store lesser drones
    [DataField, AutoNetworkedField]
    public EntProtoId Spawns = "XenoHiveWeeds";
}
