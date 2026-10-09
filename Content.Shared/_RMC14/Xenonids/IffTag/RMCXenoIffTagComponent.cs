using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.IffTag;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCXenoIffTagSystem))]
public sealed partial class RMCXenoIffTagComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<IFFFactionComponent>> Factions = new();
}
