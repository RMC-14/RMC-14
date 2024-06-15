using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoActivePheromonesComponent : Component
{
    public HashSet<Entity<XenoComponent>> Receivers = new();

    [DataField, AutoNetworkedField]
    public XenoPheromones Pheromones;
}
