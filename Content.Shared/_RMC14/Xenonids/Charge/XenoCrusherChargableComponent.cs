using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoCrusherChargableComponent : Component
{
    [DataField]
    public bool InstantDestroy = false;

    [DataField]
    public DamageSpecifier? SetDamage;
}
