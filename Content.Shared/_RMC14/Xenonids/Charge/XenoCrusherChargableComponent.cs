using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoCrusherChargableComponent : Component
{
    [DataField]
    public bool InstantDestroy = false;

    [DataField]
    public bool PassOnDestroy = false;

    [DataField]
    public DamageSpecifier? SetDamage;

    [DataField]
    public FixedPoint2? DestroyDamage;

    [DataField]
    public float? ThrowRange;
}
