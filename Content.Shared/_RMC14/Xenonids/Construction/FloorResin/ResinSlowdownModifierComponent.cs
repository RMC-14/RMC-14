using Content.Shared._RMC14.Xenonids.Weeds;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.FloorResin;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class ResinSlowdownModifierComponent : Component
{
    [DataField]
    public float OutsiderSpeedModifier;

    [DataField]
    public float OutsiderSpeedModifierArmor;
}
