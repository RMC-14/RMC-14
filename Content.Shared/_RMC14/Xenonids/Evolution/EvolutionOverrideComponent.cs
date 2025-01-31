using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem), typeof(SharedXenoHiveSystem))]
public sealed partial class EvolutionOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount;
}
