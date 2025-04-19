using Content.Shared._RMC14.Xenonids.Weeds;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.FloorResin;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class ResinSpeedupModifierComponent : Component
{

    [DataField]
    public float HiveSpeedModifier;
}
