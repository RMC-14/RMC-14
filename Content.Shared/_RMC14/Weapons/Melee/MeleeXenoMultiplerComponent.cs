using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMMeleeWeaponSystem))]
public sealed partial class MeleeXenoMultiplerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier = 1.5f;
}
