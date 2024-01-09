using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoActivePheromonesComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoPheromones Pheromones;

    public override bool SessionSpecific => true;
}
