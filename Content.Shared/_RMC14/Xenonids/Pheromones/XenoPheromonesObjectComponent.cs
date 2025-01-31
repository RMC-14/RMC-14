using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

// Component for adding pheromones to objects, e.g. spore resin fruit
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoPheromonesObjectComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoPheromones Pheromones = XenoPheromones.Recovery;
}
