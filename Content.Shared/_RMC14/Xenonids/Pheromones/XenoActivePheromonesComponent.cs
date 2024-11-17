using Content.Shared._RMC14.Xenonids.HiveLeader;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem), typeof(HiveLeaderSystem))]
public sealed partial class XenoActivePheromonesComponent : Component
{
    public HashSet<Entity<XenoComponent>> Receivers = new();

    [DataField, AutoNetworkedField]
    public XenoPheromones Pheromones;
}
