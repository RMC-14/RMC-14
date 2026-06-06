using Content.Shared._RMC14.Xenonids;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveBoonSystem), typeof(XenoSystem))]
public sealed partial class HiveBoonAggressionComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Damage = 5;
}
